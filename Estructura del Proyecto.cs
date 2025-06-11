var context = _httpContextAccessor.HttpContext;
if (context == null)
{
    Console.WriteLine("⚠ HttpContext is null. Logging will not be captured.");
}
