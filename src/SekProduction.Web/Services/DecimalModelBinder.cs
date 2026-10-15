using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SekProduction.Web.Services;

// Uygulama tr-TR kültürüyle çalışır, yani ondalık ayırıcı virgüldür. Formdan "1850.00" gibi
// noktalı bir değer gelirse varsayılan bağlayıcı noktayı binlik ayırıcı sayıp 185000 üretiyor.
// Bu bağlayıcı iki yazımı da doğru okur.
public class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var value = valueResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Geçerli bir sayı girin.");
            }

            return Task.CompletedTask;
        }

        // Önce nokta ondalıklı (binlik ayırıcı kabul edilmez), sonra tr-TR yazımı denenir.
        const NumberStyles invariantStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
        if (decimal.TryParse(value, invariantStyles, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Geçerli bir sayı girin.");
        }

        return Task.CompletedTask;
    }
}

public class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        return context.Metadata.UnderlyingOrModelType == typeof(decimal) ? new DecimalModelBinder() : null;
    }
}
