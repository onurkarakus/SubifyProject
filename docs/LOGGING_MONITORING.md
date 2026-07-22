# Subify - Logging & Monitoring Stratejisi

Bu doküman, Subify uygulamasının loglama ve izleme yaklaşımını detaylandırır.

> **Referanslar:**
>
> - [DEPLOYMENT_DIAGRAM.md](./diagrams/DEPLOYMENT_DIAGRAM.md)
> - [PRD - Observability Section](./Subify.Web.Uygulamasi.v2.PRD.md)

---

## 📋 Genel Bakış

```
┌─────────────────────────────────────────────────────────────────┐
│                        Application                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐              │
│  │   Serilog   │  │OpenTelemetry│  │ Health Check│              │
│  │   (Logs)    │  │  (Traces)   │  │  (/health)  │              │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘              │
└─────────┼────────────────┼────────────────┼─────────────────────┘
          │                │                │
          ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    OpenTelemetry Collector                       │
│         (Receives logs, traces, metrics via OTLP)                │
└──────────────────────────┬──────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
      ┌─────────┐    ┌─────────┐    ┌─────────┐
      │   Seq   │    │ Jaeger  │    │Prometheus│
      │ (Logs)  │    │(Traces) │    │(Metrics) │
      └─────────┘    └─────────┘    └─────────┘
```

---

## 📝 Serilog Konfigürasyonu

### appsettings.json

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File",
      "Serilog.Sinks.Seq"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/subify-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq:5341"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithExceptionDetails"
    ],
    "Properties": {
      "Application": "Subify.Api"
    }
  }
}
```

### Program.cs

```csharp
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
);

// Request logging middleware
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        diagnosticContext.Set("UserId", httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    };
});
```

---

## 📊 Log Seviyeleri ve Kullanım

| Seviye          | Kullanım              | Örnek                                 |
| --------------- | --------------------- | ------------------------------------- |
| **Verbose**     | Detaylı debug bilgisi | Query sonuçları, cache hit/miss       |
| **Debug**       | Geliştirme bilgisi    | Method entry/exit, değişken değerleri |
| **Information** | Normal operasyonlar   | User login, subscription created      |
| **Warning**     | Potansiyel sorunlar   | Rate limit yaklaşıyor, deprecated API |
| **Error**       | Hatalar               | Exception, API failure                |
| **Fatal**       | Kritik hatalar        | Uygulama başlatılamıyor               |

---

## 📋 Loglanacak Olaylar

### 🔐 Authentication Events

| Olay                   | Seviye      | Log Message Template                                          |
| ---------------------- | ----------- | ------------------------------------------------------------- |
| Login Success          | Information | `User {UserId} logged in from {IpAddress}`                    |
| Login Failed           | Warning     | `Failed login attempt for {Email} from {IpAddress}`           |
| Account Locked         | Warning     | `Account {Email} locked after {AttemptCount} failed attempts` |
| Token Refresh          | Debug       | `Token refreshed for {UserId}`                                |
| Logout                 | Information | `User {UserId} logged out`                                    |
| Password Reset Request | Information | `Password reset requested for {Email}`                        |

```csharp
// Örnek
_logger.LogInformation("User {UserId} logged in from {IpAddress}", user.Id, ipAddress);
_logger.LogWarning("Failed login attempt for {Email} from {IpAddress}. Attempt {Count}/5", email, ip, count);
```

### 📦 Subscription Events

| Olay          | Seviye      | Log Message Template                                      |
| ------------- | ----------- | --------------------------------------------------------- |
| Created       | Information | `Subscription {SubscriptionId} created for user {UserId}` |
| Updated       | Information | `Subscription {SubscriptionId} updated: {Changes}`        |
| Archived      | Information | `Subscription {SubscriptionId} archived by user {UserId}` |
| Limit Reached | Information | `User {UserId} reached subscription limit ({Count}/3)`    |

```csharp
_logger.LogInformation("Subscription {SubscriptionId} created for user {UserId}", sub.Id, userId);
_logger.LogInformation("User {UserId} reached subscription limit ({Count}/3)", userId, count);
```

### 💳 Payment Events

| Olay             | Seviye      | Log Message Template                                     |
| ---------------- | ----------- | -------------------------------------------------------- |
| Checkout Created | Information | `Checkout session {SessionId} created for user {UserId}` |
| Payment Success  | Information | `Payment completed for user {UserId}, plan: {Plan}`      |
| Payment Failed   | Warning     | `Payment failed for user {UserId}: {Reason}`             |
| Webhook Received | Debug       | `RevenueCat webhook received: {EventType}`               |
| Upgrade          | Information | `User {UserId} upgraded to {Plan}`                       |
| Downgrade        | Information | `User {UserId} downgraded to free`                       |

```csharp
_logger.LogInformation("Payment completed for user {UserId}, plan: {Plan}", userId, plan);
_logger.LogWarning("Payment failed for user {UserId}: {Reason}", userId, reason);
```

### 🤖 AI Events

| Olay              | Seviye      | Log Message Template                                        |
| ----------------- | ----------- | ----------------------------------------------------------- |
| Request Started   | Debug       | `AI analysis started for user {UserId}`                     |
| Request Completed | Information | `AI analysis completed for user {UserId} in {DurationMs}ms` |
| Rate Limited      | Warning     | `AI rate limit hit for user {UserId}`                       |
| Error             | Error       | `AI service error for user {UserId}: {Error}`               |

```csharp
_logger.LogInformation("AI analysis completed for user {UserId} in {DurationMs}ms", userId, sw.ElapsedMilliseconds);
_logger.LogWarning("AI rate limit hit for user {UserId}", userId);
```

### 📧 Notification Events

| Olay         | Seviye      | Log Message Template                            |
| ------------ | ----------- | ----------------------------------------------- |
| Email Sent   | Information | `Email sent to {Email}, template: {Template}`   |
| Email Failed | Error       | `Failed to send email to {Email}: {Error}`      |
| Push Sent    | Information | `Push notification sent to user {UserId}`       |
| Push Failed  | Error       | `Failed to send push to user {UserId}: {Error}` |

### ⏰ Background Job Events

| Olay          | Seviye      | Log Message Template                                                 |
| ------------- | ----------- | -------------------------------------------------------------------- |
| Job Started   | Information | `Job {JobName} started`                                              |
| Job Completed | Information | `Job {JobName} completed in {DurationMs}ms, processed {Count} items` |
| Job Failed    | Error       | `Job {JobName} failed: {Error}`                                      |

```csharp
_logger.LogInformation("Job {JobName} completed in {DurationMs}ms, processed {Count} items", jobName, duration, count);
```

---

## 🔍 OpenTelemetry Konfigürasyonu

### Traces

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("Subify.Api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(o => o.SetDbStatementForText = true)
            .AddRedisInstrumentation()
            .AddSource("Subify.Api")
            .AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(Configuration["Otel:Endpoint"]);
            });
    });
```

### Metrics

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("Subify.Api"))
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter();
    });
```

### Custom Metrics

```csharp
// Örnek: Subscription oluşturma metriği
public class SubscriptionMetrics
{
    private readonly Counter<int> _subscriptionsCreated;
    private readonly Histogram<double> _subscriptionPrice;

    public SubscriptionMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Subify.Subscriptions");
        _subscriptionsCreated = meter.CreateCounter<int>(
            "subify.subscriptions.created",
            description: "Number of subscriptions created");
        _subscriptionPrice = meter.CreateHistogram<double>(
            "subify.subscriptions.price",
            unit: "TRY",
            description: "Distribution of subscription prices");
    }

    public void RecordSubscriptionCreated(string plan, string category)
    {
        _subscriptionsCreated.Add(1,
            new KeyValuePair<string, object?>("plan", plan),
            new KeyValuePair<string, object?>("category", category));
    }
}
```

---

## 💚 Health Checks

### Konfigürasyon

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "mssql", tags: new[] { "db", "ready" })
    .AddRedis(redisConnectionString, name: "redis", tags: new[] { "cache", "ready" })
    .AddUrlGroup(new Uri("https://api.openai.com"), name: "openai", tags: new[] { "external" })
    .AddUrlGroup(new Uri("https://api.revenuecat.com"), name: "revenuecat", tags: new[] { "external" })
    .AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "worker", "ready" });

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Sadece uygulama çalışıyor mu
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Health Check Response

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "mssql": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "redis": {
      "status": "Healthy",
      "duration": "00:00:00.0056789"
    },
    "openai": {
      "status": "Healthy",
      "duration": "00:00:00.1234567"
    },
    "revenuecat": {
      "status": "Healthy",
      "duration": "00:00:00.0987654"
    }
  }
}
```

---

## 🚨 Alerting Kuralları

### Kritik (Immediate)

| Koşul                      | Alert                      |
| -------------------------- | -------------------------- |
| Health check failed        | 🔴 Service Down            |
| Error rate > 5% (5 min)    | 🔴 High Error Rate         |
| Response time P95 > 2s     | 🔴 Performance Degradation |
| Database connection failed | 🔴 Database Unreachable    |

### Uyarı (Warning)

| Koşul                      | Alert              |
| -------------------------- | ------------------ |
| Error rate > 1% (15 min)   | 🟡 Elevated Errors |
| Response time P95 > 1s     | 🟡 Slow Response   |
| Memory usage > 80%         | 🟡 High Memory     |
| Disk usage > 80%           | 🟡 Low Disk Space  |
| AI service errors > 3/hour | 🟡 AI Issues       |

### Bilgi (Info)

| Koşul                     | Event            |
| ------------------------- | ---------------- |
| New user registered       | 📊 User Growth   |
| Premium upgrade           | 📊 Revenue Event |
| Daily active users change | 📊 Engagement    |

---

## 📊 Dashboard Metrikleri

### Application Metrics

| Metrik                          | Açıklama                        |
| ------------------------------- | ------------------------------- |
| `http_requests_total`           | Toplam HTTP istek sayısı        |
| `http_request_duration_seconds` | İstek süreleri (histogram)      |
| `http_requests_in_progress`     | Anlık aktif istekler            |
| `api_errors_total`              | Hata sayısı (error code'a göre) |

### Business Metrics

| Metrik                       | Açıklama                          |
| ---------------------------- | --------------------------------- |
| `subify_users_total`         | Toplam kullanıcı (plan'a göre)    |
| `subify_subscriptions_total` | Toplam abonelik (kategoriye göre) |
| `subify_payments_total`      | Ödeme sayısı (plan türüne göre)   |
| `subify_ai_requests_total`   | AI istek sayısı                   |
| `subify_monthly_spend_total` | Toplam izlenen harcama            |

### Infrastructure Metrics

| Metrik                          | Açıklama                 |
| ------------------------------- | ------------------------ |
| `process_cpu_seconds_total`     | CPU kullanımı            |
| `process_resident_memory_bytes` | Memory kullanımı         |
| `dotnet_gc_collections_total`   | GC collections           |
| `db_connections_active`         | Aktif DB bağlantıları    |
| `redis_connections_active`      | Aktif Redis bağlantıları |

---

## 🔒 PII Redaction

Loglarda kişisel bilgilerin maskelenmesi:

```csharp
// Serilog Destructuring Policy
public class PiiDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue? result)
    {
        if (value is User user)
        {
            result = factory.CreatePropertyValue(new
            {
                user.Id,
                Email = MaskEmail(user.Email), // a***@example.com
                user.Plan,
                user.Locale
            }, destructureObjects: true);
            return true;
        }

        result = null;
        return false;
    }

    private string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
```

### Sensitive Fields

| Field          | Maskeleme                     |
| -------------- | ----------------------------- |
| `email`        | `a***@example.com`            |
| `password`     | Hiç loglanmaz                 |
| `refreshToken` | `[REDACTED]`                  |
| `apiKey`       | `sk-***1234` (son 4 karakter) |
| `creditCard`   | Hiç loglanmaz                 |

---

## 🗄️ Log Retention

| Ortam       | Retention | Storage          |
| ----------- | --------- | ---------------- |
| Development | 7 gün     | Local files      |
| Staging     | 30 gün    | Seq              |
| Production  | 90 gün    | Seq + S3 archive |

### Retention Policy

```json
{
  "retentionPolicy": {
    "development": {
      "days": 7,
      "deleteArchive": true
    },
    "staging": {
      "days": 30,
      "archiveTo": null
    },
    "production": {
      "days": 90,
      "archiveTo": "s3://subify-logs/archive/"
    }
  }
}
```

---

## ✅ Monitoring Checklist

### Kurulum

- [ ] Serilog konfigürasyonu
- [ ] OpenTelemetry collector deploy
- [ ] Health endpoint'leri
- [ ] Seq/Jaeger dashboard
- [ ] Alert kuralları

### Günlük Kontrol

- [ ] Error rate normal
- [ ] Response time normal
- [ ] Health check'ler healthy
- [ ] Disk/Memory usage normal

### Haftalık Review

- [ ] Error pattern analizi
- [ ] Performance trend'leri
- [ ] Business metrics review
- [ ] Alert threshold ayarlaması
