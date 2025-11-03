using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Gateway.Configuration;

/// <summary>
/// Convención que deshabilita los endpoints de los controllers proxy para que no interfieran con YARP.
/// Los controllers proxy SOLO existen para documentación Swagger, NUNCA deben manejar peticiones reales.
/// </summary>
public class DisableProxyControllersConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            // Si el controller termina en "ProxyController", deshabilitarlo del routing
            if (controller.ControllerName.EndsWith("Proxy"))
            {
                // Marcar como excluido del API Explorer (pero Swagger ya lo descubrió en AddSwaggerGen)
                foreach (var selector in controller.Selectors)
                {
                    // No agregar rutas al endpoint routing - YARP manejará las peticiones
                    selector.AttributeRouteModel = null;
                }

                // Alternativamente, marcar acciones como no enrutables
                foreach (var action in controller.Actions)
                {
                    action.ApiExplorer.IsVisible = false; // Esto NO afecta a Swagger ya generado
                }
            }
        }
    }
}
