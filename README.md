# repeating-api-routes
Adds support for repeating api route segments with field data in asp .net core
## Explanation
The goal is to provide an implementation of a repeating route segment that can aggregate data in a straightforward manner.

I use tilda ~ to represent the word "repeat". The following route segment can be read, "Repeat item as items":

```~{item}as{items}```

The above segment would match all of the follwing segments: /itemName1/itemName2/itemName3

The data would be mapped to a single value tuple array, e.g. (string)[]

Note: that currently methods overloads are not supported. If two methods have the same name the result is a 500 error caused by the framework not knowing which should be mapped.

Limitation: currently only simple types are supported and all of the values in a repeating segment must have the same type. In other words, a (string, float)[] is not a valid action method parameter for a repeating route segment.

## Usage

### Controller Action Method Examples:
```
[HttpGet("People/~{name}as{names}")]
public async Task<IActionResult> SearchByColumn((string)[] names)
{
    return Ok();
}
```
```
[HttpGet("Search/~{column}:{value}as{values}")]
public async Task<IActionResult> SearchByColumn((string, string)[] values)
{
    return Ok();
}
```
```
[HttpGet("Vertices/~{x}:{y}:{z}as{vertices}")]
public async Task<IActionResult> SearchByColumn((float, float, float)[] vertices)
{
    return Ok();
}
```
### Registering in Startup.cs:
```
services.AddControllers(opt => opt.ModelBinderProviders.Insert(0, new RouteValueRepeatTransformer()));
```
