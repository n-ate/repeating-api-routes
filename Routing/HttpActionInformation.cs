using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RepeatingApiRoutes.Routing
{
    public class HttpActionInformation
    {
        private const int trimLength = 10; // "Controller".Length;

        public HttpActionInformation(Type controller, MethodInfo method, HttpMethodAttribute action, RouteAttribute route = null)
        {
            var template = (route?.Template + action.Template).Replace("[controller]", controller.Name.Substring(0, controller.Name.Length - trimLength));
            this.HttpMappings = new ReadOnlyDictionary<string, MethodInfo>(action.HttpMethods.ToDictionary(h => h, h => method));
            this.Name = action.Name;
            this.Order = action.Order;
            this.Segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(s => new Segment(s)).ToArray();
            this.Template = template;
            this.AnyRepeating = this.Segments.Any(s => s.IsRepeating);
        }

        public bool AnyRepeating { get; }

        public ReadOnlyDictionary<string, MethodInfo> HttpMappings { get; set; }

        public string Name { get; set; }

        public int Order { get; set; }

        public Segment[] Segments { get; set; }

        public string Template { get; set; }

        public Type GetArgumentType(string httpMethod, string name)
        {
            var argument = this.HttpMappings[httpMethod].GetParameters().FirstOrDefault(p => p.Name == name);
            return argument?.ParameterType.GetElementType();
        }

        public bool IsMatch(string httpMethod, string pathValue, out Dictionary<string, object> data)
        {
            var resultData = new List<KeyValuePair<string, object>>();
            var routeMatch = true;
            string[] segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (this.HttpMappings.ContainsKey(httpMethod))
            {
                var matchedOnce = false;
                var routeIndex = 0;
                var repeatingData = new List<ITuple>();
                for (var i = 0; i < segments.Length; i++)
                {
                    if (routeIndex < this.Segments.Length)
                    {
                        var routeSegment = this.Segments[routeIndex];
                        var segmentMatch = routeSegment.IsMatch(segments[i], out KeyValuePair<string, object>[] segmentData);
                        if (routeSegment.IsRepeating)
                        {
                            if (segmentMatch) matchedOnce = true;
                            if (!matchedOnce) routeMatch = false;// repeating sections must have at least 1 matching segment
                            if (!segmentMatch)//reset for next route segment
                            {
                                matchedOnce = false;
                                routeIndex++;
                            }
                            var repeatData = ConstructValueTuple(segmentData.Select(d => d.Value).ToArray());
                            repeatingData.Add(repeatData);
                        }
                        else
                        {
                            if (repeatingData.Any())
                            {
                                resultData.Add(GetProperlyCastRepeaterKeyValue(routeSegment.RepeatAsKey, httpMethod, repeatingData));//add the collected repeating data as a an array value
                            }
                            repeatingData.Clear();
                            routeIndex++;//increment the template segment
                            if (!segmentMatch) routeMatch = false;
                            resultData.AddRange(segmentData);
                        }
                    }
                    else routeMatch = false;
                    if (!routeMatch) break;
                }
                if (repeatingData.Any())
                {
                    resultData.Add(GetProperlyCastRepeaterKeyValue(this.Segments.Last().RepeatAsKey, httpMethod, repeatingData));//add the collected repeating data as a an array value
                }
            }
            else routeMatch = false;

            data = resultData.ToDictionary(kv => kv.Key, kv => kv.Value);
            return routeMatch;
        }

        public bool IsMatch(HttpRequest request, out Dictionary<string, object> data)
        {
            return IsMatch(request.Method, request.Path.Value, out data);
        }

        public void Merge(HttpActionInformation attribute)
        {
            this.HttpMappings = new ReadOnlyDictionary<string, MethodInfo>(
                new Dictionary<string, MethodInfo>(this.HttpMappings.Concat(attribute.HttpMappings))//combine both sets of mappings
            );
            this.Name = string.IsNullOrEmpty(this.Name) ? attribute.Name : this.Name;
            this.Order = this.Order > 0 ? this.Order : attribute.Order;
        }

        private ITuple ConstructValueTuple<T>(params T[] data)
        {
            var type = typeof(ValueTuple);
            var createMethod = type.GetMethods().FirstOrDefault(m => m.Name == nameof(ValueTuple.Create) && m.GetParameters().Length == data.Length);
            if (createMethod == null) throw new ArgumentException($"No ValueTuple with {data.Length} generic type arguments was found.");

            MethodInfo generic = createMethod.MakeGenericMethod(data.Select(d => d.GetType()).ToArray());
            var result = generic.Invoke(null, data.Cast<object>().ToArray());
            return (ITuple)result;
        }

        private KeyValuePair<string, object> GetProperlyCastRepeaterKeyValue(string valueKey, string httpMethod, List<ITuple> repeatingData)
        {
            var argumentType = GetArgumentType(httpMethod, valueKey) ?? repeatingData.First().GetType();
            var argumentValue = Array.CreateInstance(argumentType, repeatingData.Count);
            repeatingData.Select(d => Convert.ChangeType(d, argumentType)).ToArray().CopyTo(argumentValue, 0);
            return new KeyValuePair<string, object>(valueKey, argumentValue);
        }
    }
}
