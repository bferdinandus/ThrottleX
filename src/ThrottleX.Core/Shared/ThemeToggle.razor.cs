using Microsoft.JSInterop;

namespace ThrottleX.Core.Shared;

public partial class ThemeToggle
{
    private string _currentTheme = "auto";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                var theme = await JSRuntime.InvokeAsync<string>("themeManager.getTheme");
                if (!string.IsNullOrEmpty(theme))
                {
                    _currentTheme = theme;
                    StateHasChanged();
                }
            }
            catch
            {
                // In prerendering or JS unavailable
            }
        }
    }

    private async Task SetTheme(string theme)
    {
        _currentTheme = theme;
        try
        {
            await JSRuntime.InvokeVoidAsync("themeManager.setTheme", theme);
        }
        catch
        {
            // Ignored if JS unavailable
        }
    }
}
