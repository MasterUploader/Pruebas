// En Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveSession", policy =>
        policy.RequireAssertion(ctx =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return false;

            // resolve service
            var sp = builder.Services.BuildServiceProvider();
            var svc = sp.GetRequiredService<ISessionManagerService>();
            return svc.IsSessionActiveAsync(userId);
        })
    );
});
