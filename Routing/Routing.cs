using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace RepeatingApiRoutes.Routing
{
    public static class Routing
    {
        public static HttpActionInformation GetMatchingRouteOfHighestPriority(HttpRequest requestContext, ReadOnlyDictionary<string, HttpActionInformation>.ValueCollection definedRoutes, out Dictionary<string, object> requestData)
        {
            HttpActionInformation result = null;
            var matchingRoutes = GetMatchingRoutes(requestContext, definedRoutes);
            if (matchingRoutes.Any())
            {
                var priorityPair = matchingRoutes.OrderBy(r => r.Item1.Order).First();
                result = priorityPair.Item1;
                requestData = priorityPair.Item2;
            }
            else requestData = null;
            return result;
        }

        public static HttpActionInformation GetMatchingRouteOfHighestPriority(HttpRequest requestContext, ReadOnlyDictionary<string, HttpActionInformation>.ValueCollection definedRoutes)
        {
            return GetMatchingRouteOfHighestPriority(requestContext, definedRoutes, out Dictionary<string, object> data);
        }

        public static List<(HttpActionInformation, Dictionary<string, object>)> GetMatchingRoutes(HttpRequest requestContext, IEnumerable<HttpActionInformation> definedRoutes)
        {
            var matchingRoutes = new List<(HttpActionInformation, Dictionary<string, object>)>();
            foreach (var route in definedRoutes)
            {
                var match = route.IsMatch(requestContext, out Dictionary<string, object> data);
                if (match) matchingRoutes.Add((route, data));
            }

            return matchingRoutes;
        }

        public static ReadOnlyDictionary<string, HttpActionInformation> GetRouteDictionary(Assembly assembly)
        {
            var result = new Dictionary<string, HttpActionInformation>();
            foreach (Type type in assembly.GetTypes())
            {
                var apiControllers = type.GetCustomAttributes<ApiControllerAttribute>(true);
                if (apiControllers.Any())
                {
                    var methods = type.GetMethods();
                    foreach (var method in methods)
                    {
                        var actions = method.GetCustomAttributes<HttpMethodAttribute>(true);
                        if (actions.Any())
                        {
                            foreach (var action in actions)
                            {
                                var controller = type.Name;
                                var classRoutes = type.GetCustomAttributes<RouteAttribute>(true);
                                if (classRoutes.Any())
                                {
                                    foreach (var route in classRoutes)
                                    {
                                        var httpMethod = new HttpActionInformation(type, method, action, route);
                                        if (result.ContainsKey(httpMethod.Template))
                                        {
                                            result[httpMethod.Template].Merge(httpMethod);
                                        }
                                        else result.Add(httpMethod.Template, httpMethod);
                                    }
                                }
                                else
                                {
                                    var httpMethod = new HttpActionInformation(type, method, action);
                                    if (result.ContainsKey(httpMethod.Template))
                                    {
                                        result[httpMethod.Template].Merge(httpMethod);
                                    }
                                    else result.Add(httpMethod.Template, httpMethod);
                                }
                            }
                        }
                    }
                }
            }
            return new ReadOnlyDictionary<string, HttpActionInformation>(result);
        }
    }
}
