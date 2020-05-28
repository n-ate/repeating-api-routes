using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RepeatingApiRoutes.Routing
{
    public class RouteValueRepeatTransformer : DynamicRouteValueTransformer, IModelBinderProvider, IModelBinder
    {
        public ReadOnlyDictionary<string, HttpActionInformation> HandledRoutes { get; private set; } = new ReadOnlyDictionary<string, HttpActionInformation>(
            new Dictionary<string, HttpActionInformation>(
                Routing.GetRouteDictionary(Assembly.GetExecutingAssembly()).Where(kv => kv.Value.AnyRepeating)
            )
        );

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var key = bindingContext.ModelName;
            if (bindingContext.ActionContext.RouteData.Values.TryGetValue(key, out var value))
            {
                if (bindingContext.ModelType == value.GetType())
                {
                    bindingContext.Result = ModelBindingResult.Success(value);
                }
            }
            return Task.CompletedTask;
        }

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            return this;
        }

        public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            HttpActionInformation matchingRoute = Routing.GetMatchingRouteOfHighestPriority(httpContext.Request, this.HandledRoutes.Values, out Dictionary<string, object> requestData);
            if (matchingRoute != null)
            {
                var routeValues = new RouteValueDictionary();
                var httpMethod = httpContext.Request.Method;
                routeValues.Add("controller", values["controller"]);
                routeValues.Add("action", matchingRoute.HttpMappings[httpMethod].Name);
                foreach (var kv in requestData) routeValues[kv.Key] = kv.Value;//add all fields
                values = routeValues;
            }
            await Task.CompletedTask;
            return values;
        }
    }
}
