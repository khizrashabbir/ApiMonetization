 System Architecture

                    ┌─────────────────┐
                    │     CLIENTS     │
                    │ (Web/Mobile/API)│
                    └────────┬────────┘
                             │ HTTP + API Key
                             ▼
    ┌─────────────────────────────────────────────────────────┐
    │                 API GATEWAY                             │
    │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │
    │  │ Rate Limit  │ │ Auth Check  │ │ Request Logging │   │
    │  │ Middleware  │ │ Middleware  │ │   Middleware    │   │
    │  └─────────────┘ └─────────────┘ └─────────────────┘   │
    └─────────────────────┬───────────────────────────────────┘
                          │
                          ▼
    ┌─────────────────────────────────────────────────────────┐
    │               WEB API LAYER                             │
    │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │
    │  │ Customers   │ │   Tiers     │ │     Usage       │   │
    │  │ Controller  │ │ Controller  │ │   Controller    │   │
    │  └─────────────┘ └─────────────┘ └─────────────────┘   │
    └─────────────────────┬───────────────────────────────────┘
                          │
                          ▼
    ┌─────────────────────────────────────────────────────────┐
    │             BUSINESS LOGIC LAYER                        │
    │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │
    │  │ Rate Limit  │ │  Customer   │ │ Usage Tracking  │   │
    │  │  Service    │ │   Service   │ │    Service      │   │
    │  └─────────────┘ └─────────────┘ └─────────────────┘   │
    └─────────────────────┬───────────────────────────────────┘
                          │
                          ▼
    ┌─────────────────────────────────────────────────────────┐
    │               DATA ACCESS LAYER                         │
    │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────┐   │
    │  │ Customer    │ │ Usage Log   │ │  Rate Limit     │   │
    │  │ Repository  │ │ Repository  │ │   Repository    │   │
    │  └─────────────┘ └─────────────┘ └─────────────────┘   │
    │           ┌─────────────────────────────┐               │
    │           │      Dapper + SQL           │               │
    │           └─────────────────────────────┘               │
    └─────────────────────┬───────────────────────────────────┘
                          │
                          ▼
            ┌─────────────────────────────────┐
            │        SQL SERVER               │
            │     (5 Main Tables)             │
            └─────────────────────────────────┘
```
 Request Flow 

```
Client Request (API Key) 
       │
       ▼
┌─────────────┐
│ Middleware  │ ──► Auth Check ──► Rate Limit Check
│ Pipeline    │
└──────┬──────┘
       │ ✓ Passed
       ▼
┌─────────────┐
│ Controller  │ ──► Business Logic
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Repository  │ ──► Database Query/Update
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Response +  │ ──► Log Usage + Return Data
│ Usage Log   │
└─────────────┘
```

Entity Relationship Diagram (ERD)

```
            ┌─────────────────┐
            │      TIERS      │
            │ ─────────────── │
            │ • Id (PK)       │
            │ • Name          │
            │ • RateLimit     │
            │ • MonthlyQuota  │
            │ • MonthlyPrice  │
            └─────────┬───────┘
                      │ 1
                      │
                      │ M
            ┌─────────▼───────┐
            │   CUSTOMERS     │
            │ ─────────────── │
            │ • Id (PK)       │
            │ • Name          │
            │ • Email         │
            │ • ApiKey        │
            │ • TierId (FK)   │
            │ • MonthUsage    │
            └─────┬───────────┘
                  │ 1
         ┌────────┼────────┐
         │ M      │ M      │ M
         ▼        ▼        ▼
┌────────────┐ ┌──────────────┐ ┌─────────────────┐
│API_USAGE   │ │RATE_LIMIT    │ │MONTHLY_USAGE    │
│_LOGS       │ │_TRACKERS     │ │_SUMMARIES       │
│──────────  │ │────────────  │ │───────────────  │
│• Id (PK)   │ │• Id (PK)     │ │• Id (PK)        │
│• CustomerId│ │• CustomerId  │ │• CustomerId (FK)│
│• Endpoint  │ │• WindowStart │ │• Year           │
│• Timestamp │ │• ReqCount    │ │• Month          │
│• Cost      │ │• LastReq     │ │• TotalRequests  │
└────────────┘ └──────────────┘ │• TotalCost      │
                                └─────────────────┘
```

