# التعديلات اليدوية على الملفات الموجودة

بعد نسخ الملفات الجديدة، عدّل فقط الملفات الأربعة التالية.

---

## 1) src/OFOQ.Market.Application/DependencyInjection.cs

### أضف هذه using بعد using الخاص بـ Checkout

```csharp
using OFOQ.Market.Application.Commerce.Payments.CreateIntent;
using OFOQ.Market.Application.Commerce.Payments.GetAvailableMethods;
using OFOQ.Market.Application.Commerce.Payments.GetIntent;
using OFOQ.Market.Application.Commerce.Payments.RetryIntent;
```

### وبعد تسجيل CheckoutHandler أضف

```csharp
        services.AddScoped<
            GetAvailablePaymentMethodsHandler>();

        services.AddScoped<
            CreatePaymentIntentHandler>();

        services.AddScoped<
            GetPaymentIntentHandler>();

        services.AddScoped<
            RetryPaymentIntentHandler>();
```

---

## 2) src/OFOQ.Market.Infrastructure/DependencyInjection.cs

بعد:

```csharp
        services.AddScoped<
            ITenantPaymentCapabilityRepository,
            TenantPaymentCapabilityRepository>();
```

أضف:

```csharp
        services.AddScoped<
            IPaymentCreationLockRepository,
            PaymentCreationLockRepository>();
```

لا تحتاج using جديد لأن الملف يحتوي أصلًا على:

```csharp
using OFOQ.Market.Application.Common.Persistence;
using OFOQ.Market.Infrastructure.Persistence.Repositories;
```

---

## 3) src/OFOQ.Market.Api/Program.cs

بعد:

```csharp
app.MapCheckoutEndpoints();
```

أضف:

```csharp
app.MapPaymentEndpoints();
```

ولا تحتاج using جديد لأن Program.cs يحتوي أصلًا على:

```csharp
using OFOQ.Market.Api.Endpoints.Commerce;
```

---

## 4) tests/OFOQ.Market.Api.Tests/Support/MarketApiFactory.cs

### أ) في قسم Remove real persistence registrations

بعد إزالة ICheckoutLockRepository:

```csharp
                services.RemoveAll<
                    ICheckoutLockRepository>();
```

أضف:

```csharp
                services.RemoveAll<
                    IPaymentRepository>();

                services.RemoveAll<
                    IPaymentIntentRepository>();

                services.RemoveAll<
                    ITenantPaymentMethodRepository>();

                services.RemoveAll<
                    ITenantPaymentCapabilityRepository>();

                services.RemoveAll<
                    IPaymentCreationLockRepository>();
```

### ب) في قسم Commerce fakes

بعد:

```csharp
                services.AddScoped<
                    ICheckoutLockRepository,
                    InMemoryCheckoutLockRepository>();
```

أضف:

```csharp
                services.AddSingleton<
                    InMemoryPaymentStore>();

                services.AddScoped<
                    IPaymentRepository,
                    InMemoryPaymentRepository>();

                services.AddSingleton<
                    InMemoryPaymentIntentStore>();

                services.AddScoped<
                    IPaymentIntentRepository,
                    InMemoryPaymentIntentRepository>();

                services.AddSingleton<
                    InMemoryTenantPaymentMethodStore>();

                services.AddScoped<
                    ITenantPaymentMethodRepository,
                    InMemoryTenantPaymentMethodRepository>();

                services.AddSingleton<
                    InMemoryTenantPaymentCapabilityStore>();

                services.AddScoped<
                    ITenantPaymentCapabilityRepository,
                    InMemoryTenantPaymentCapabilityRepository>();

                services.AddScoped<
                    IPaymentCreationLockRepository,
                    InMemoryPaymentCreationLockRepository>();
```

لا تحتاج using جديد لأن MarketApiFactory.cs يحتوي أصلًا على:

```csharp
using OFOQ.Market.Application.Common.Persistence;
```

---

# بعد انتهاء النسخ والتعديلات

نفذ فقط أولًا:

```powershell
cd C:\OFOQ.Market1
dotnet build .\OFOQ.Market.sln -c Debug
```

ولا تعمل Migration لهذه المرحلة.

إذا نجح Build شغّل اختبارات Payment الجديدة:

```powershell
dotnet test .\OFOQ.Market.sln -c Debug --no-build --filter "FullyQualifiedName~Payments|FullyQualifiedName~PaymentApi"
```

ثم full suite:

```powershell
dotnet test .\OFOQ.Market.sln -c Debug --no-build
```
