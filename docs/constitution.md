# TabFlow Anayasa (Constitution)

## Temel Kurallar

### 1. Radikal Kararlar — Geriye Dönük Uyumluluk Kaygısı Yok
- İleriye dönük mimari kararlar alınır
- Legacy code'u korumak yerine refactor/decompose tercih edilir
- Breaking changes kabul edilebilir, ancak gerekçeli olmalı
- Teknik borç (technical debt) yönetilmeli, biriktirilmemeli

### 2. Geçici Çözüm Yasak — Derin Analiz, Kalıcı Plan, Sıralı Uygulama
- Her problem için kök neden analizi yapılır
- Geçici hack'ler ve work-around'lar yasaktır
- Her çözüm kalıcı, ölçeklenebilir ve bakımı kolay olmalı
- Karmaşık problemler küçük fazlara bölünerek sırayla çözülür

### 3. Docs-First
- **docs/ klasörü değişmez** — sadece kalıcı dokümantasyon
- **templatedocs/ geçici inşaat planları** — geçici planlar ve notlar
- Kod değişikliklerinden önce dokümantasyon güncellenir
- API dokümantasyonu otomatik üretilmeli veya senkronize tutulmalı

### 4. Her Adımda: En Doğru, Kanıtlanmış, Konvansiyonel, Basit, Az Bağımlılık, Optimize, Uzun Ömürlü, Güncel, Tekrarsız, Standart

#### En Doğru
- Problemi doğru tanımla
- Doğru teknoloji seçimi yap
- Domain-driven design prensiplerini uygula

#### Kanıtlanmış
- Production'da test edilmiş teknolojiler kullan
- Mature framework'ler ve library'ler tercih et
- Beta/experimental teknolojilerden kaçın

#### Konvansiyonel
- Industry standard pattern'leri kullan
- Konvansiyonel naming convention'larını takip et
- Microsoft .NET guidelines'a uygun kod yaz

#### Basit
- KISS (Keep It Simple, Stupid) prensibi
- Unnecessary abstraction'lardan kaçın
- YAGNI (You Aren't Gonna Need It) prensibi

#### Az Bağımlılık
- Minimum external dependency
- Dependency injection kullan
- Loose coupling, high cohesion

#### Optimize
- Performance gerektiren yerlerde optimize et
- Premature optimization'dan kaçın
- Measure before optimize

#### Uzun Ömürlü
- Maintainable code yaz
- SOLID prensiplerine uygula
- Test coverage sağla

#### Güncel
- LTS .NET sürümlerini kullan
- Güncel security patch'leri uygula
- Deprecate edilmiş API'lerden kaçın

#### Tekrarsız
- DRY (Don't Repeat Yourself) prensibi
- Common kodları shared library'e taşı
- Code generation/template kullan

#### Standart
- Coding standards takip et
- Code review yap
- Linting ve formatting araçları kullan

## Mimari Prensipleri

### Layered Architecture
- **Platform Host**: Blazor Web App (Interactive Server) — kontrol düzlemi
- **Platform Worker**: BackgroundService — tenant provisioning
- **Tenant Host**: Blazor Web App (mixed render) — tenant UI
- **Shared**: TabFlow.Shared paketi — domain ve infrastructure

### Storage
- PostgreSQL 17
- İki ayrı DB: tabflow_platform + tabflow_{tenant-code}
- EF Core migrations schema yönetimi

### Authentication
- ASP.NET Core Identity (cookie)
- Platform ve tenant ayrı store
- Authorization policies

### Realtime
- In-process System.Threading.Channels event bus
- ESP32 device WebSocket: /ws/tables/{tableNumber}

## Geliştirme Süreci

### Faz Bazlı Geliştirme
- Her faz tamamlanmadan sonraki faza geçilmez
- Her fazın acceptance criteria'ları olur
- Fazlar sırayla ilerler

### Code Review
- Tüm PR'lar code review'dan geçer
- Anayasa kurallarına uyulmalı
- Build başarılı olmalı
- Test'ler geçmeli

### Testing
- Unit test'ler kritik business logic için
- Integration test'ler API endpoint'ler için
- E2E test'ler kritik user flow'lar için

## Deployment

### Environment Strategy
- Development: Local development
- Staging: Pre-production testing
- Production: Live environment

### CI/CD
- Automated build
- Automated testing
- Automated deployment (manual approval)

### Monitoring
- OpenTelemetry tracing
- Structured logging
- Health checks

## Değişiklik Yönetimi

### Anayasa Değişikliği
- Anayasa sadece konsensüs ile değiştirilir
- Değişiklik gerekçeli olmalı
- Tüm team üyeleri bilgilendirilmeli

### Breaking Changes
- Breaking changes versiyon numarasını artırır
- Migration path sağlanmalı
- Dokümantasyon güncellenmeli

## Dokümantasyon Yapısı

### docs/ (Değişmez)
- constitution.md (bu dosya)
- architecture.md
- api.md
- deployment.md

### templatedocs/ (Geçici)
- entity-reference.md
- service-reference.md
- api-reference.md
- implementation-patterns.md
- Diğer geçici planlar ve notlar

---

**Bu anayasa TabFlow projesinin geliştirme sürecinde rehber olarak kullanılır.**
