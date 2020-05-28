using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SIMA.Data.API.Routing.Segments;
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
            var template = (route?.Template.TrimEnd('/') + '/' + action.Template).Replace("[controller]", controller.Name.Substring(0, controller.Name.Length - trimLength));
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

        public bool IsMatch(string httpMethod, string requestPath, out Dictionary<string, object> data)
        {
            var resultData = new List<KeyValuePair<string, object>>();
            var routeMatch = true;
            string[] requestSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (this.HttpMappings.ContainsKey(httpMethod))
            {
                var requestIndex = 0;
                var routeIndex = 0;
                while (requestIndex < requestSegments.Length && routeIndex < this.Segments.Length && routeMatch)
                {
                    var requestSegment = requestSegments[requestIndex];
                    var routeSegment = this.Segments[routeIndex];

                    var requestData = routeSegment.GetMatchingRequestSegments(requestSegments.Skip(requestIndex).ToArray());

                    if (requestData.Length > 0)
                    {
                        requestIndex += requestData.Length;
                        routeIndex++;
                        if (routeSegment.IsRepeating)
                        {
                            var tupleValueType = GetArgumentType(httpMethod, routeSegment.RepeatAsKey).GenericTypeArguments.First();
                            var tuples = requestData.Select(d => ConstructValueTuple(tupleValueType, d.Select( kv => kv.Value).ToArray()));
                            var dataKeyValue = GetProperlyCastRepeaterKeyValue(routeSegment.RepeatAsKey, httpMethod, tuples);
                            resultData.Add(dataKeyValue);
                        }
                        else resultData.AddRange(requestData.First());
                    }
                    else routeMatch = false;
                }
                if (requestIndex != requestSegments.Length || routeIndex > this.Segments.Length) routeMatch = false;//if either the request path or the route path has remaining segments then the routes do not match
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

        private ITuple ConstructValueTuple(Type tupleValueType, params object[] data)
        {
            var type = typeof(ValueTuple);
            var createMethod = type.GetMethods().FirstOrDefault(m => m.Name == nameof(ValueTuple.Create) && m.GetParameters().Length == data.Length);
            if (createMethod == null) throw new ArgumentException($"No ValueTuple with {data.Length} generic type arguments was found.");

            MethodInfo generic = createMethod.MakeGenericMethod(data.Select(d => tupleValueType).ToArray());
            var result = generic.Invoke(null, data.Select(d=> Convert.ChangeType(d, tupleValueType)).Cast<object>().ToArray());
            return (ITuple)result;
        }

        private ITuple ConstructValueTuple<T>(params T[] data)
        {
            return ConstructValueTuple(typeof(T), data);
        }

        private KeyValuePair<string, object> GetProperlyCastRepeaterKeyValue(string valueKey, string httpMethod, IEnumerable<ITuple> repeatingData)
        {
            var argumentType = GetArgumentType(httpMethod, valueKey) ?? repeatingData.First().GetType();
            var argumentValue = Array.CreateInstance(argumentType, repeatingData.Count());
            repeatingData.Select(d => Convert.ChangeType(d, argumentType)).ToArray().CopyTo(argumentValue, 0);
            return new KeyValuePair<string, object>(valueKey, argumentValue);
        }
    }
}
