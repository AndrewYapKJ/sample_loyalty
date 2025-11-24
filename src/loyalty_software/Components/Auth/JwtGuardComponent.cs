using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace loyalty_sfotware.Components.Auth
{
    public class JwtGuardComponent : ComponentBase
    {
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] public NavigationManager Navigation { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
            var token = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "jwtToken");
            var isLoginPage = Navigation.Uri.EndsWith("/") || Navigation.Uri.Contains("/login");
            if (string.IsNullOrEmpty(token) && !isLoginPage)
            {
                Navigation.NavigateTo("/", true);
            }
        }
    }
}
