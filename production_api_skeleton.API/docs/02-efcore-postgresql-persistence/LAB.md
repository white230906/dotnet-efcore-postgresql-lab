# Mini-project 02: EF Core, PostgreSQL & Optimistic Concurrency

## 1. Mục tiêu

Mở rộng source đã hoàn thành ở Mini-project 01 để thay kho dữ liệu in-memory bằng PostgreSQL thật.

Technical core của lab:

- Tách riêng project `Persistence`.
- Cấu hình `DbContext` và Npgsql bằng DI.
- Mapping entity bằng `IEntityTypeConfiguration<TEntity>`.
- Dùng migration để quản lý schema và seed data.
- Tách Application khỏi EF Core qua repository abstraction và Unit of Work.
- Hiểu tracking, `AsNoTracking`, scope của `DbContext` và `SaveChangesAsync`.
- Dùng transaction cho một use case ghi nhiều thay đổi liên quan.
- Dùng PostgreSQL `xmin` làm concurrency token.
- Chuyển xung đột cập nhật thành HTTP `409 Conflict`.
- Integration test với PostgreSQL thật bằng Testcontainers.

Sau lab này, bạn phải giải thích được đường đi:

```text
HTTP request
→ Carter endpoint
→ Application service
→ Repository abstraction
→ EF Core repository
→ ApplicationDbContext
→ PostgreSQL
→ HTTP response
```

## 2. Production motivation và bằng chứng

Các cơ chế của lab được chọn từ những phần đã xác minh trong `buildx-be`:

- `ApplicationDbContext` và tự động nạp entity configuration:
  `src/bmt-be.persistence/ApplicationDbContext.cs`.
- Đăng ký Npgsql, DbContext, interceptor và repository:
  `src/bmt-be.persistence/dependencyInjection/extensions/ServiceCollectionExtensions.cs`.
- Mapping bằng `IEntityTypeConfiguration<TEntity>`:
  `src/bmt-be.persistence/configurations`.
- Unit of Work và transaction:
  `src/bmt-be.persistence/repositories/EFUnitOfWork.cs`.
- PostgreSQL row version bằng `.IsRowVersion()` và `xmin`:
  `UserConfiguration`, `AiPromptTrialConfiguration` và các migration tương ứng.
- Chuẩn hóa `DbUpdateConcurrencyException` thành lỗi conflict:
  `src/bmt-be.api/middlewares/ExceptionHandlingMiddleware.cs`.

Lab chỉ tái tạo áp lực kỹ thuật. Không sao chép entity, payload hoặc nghiệp vụ riêng của production.

## 3. Source dùng làm điểm xuất phát

Sao chép toàn bộ source đã hoàn thành của Mini-project 01 sang một repository mới.

Tên repo đề xuất:

```text
dotnet-efcore-postgresql-lab
```

Được giữ lại từ Lab 1:

- API host và composition root.
- Carter endpoints.
- Application, Domain, Contract và Presentation.
- API versioning, Swagger và Options.
- Correlation ID, Problem Details và Serilog.
- Integration test bằng `WebApplicationFactory<Program>`.

Phải thay đổi:

- Xóa `ConcurrentDictionary` và in-memory service.
- Thêm project `Persistence`.
- Chuyển các method đọc/ghi sang bất đồng bộ.
- Chuyển lifetime của service từ `Singleton` sang `Scoped`.
- Chạy test với PostgreSQL thật.

Không bắt buộc đổi tên toàn bộ project sau khi copy. Việc rename không phải mục tiêu của lab.

## 4. Bài toán tối thiểu

Tiếp tục dùng domain trung lập `WorkItem`:

```text
WorkItem
- Id          Guid
- Title       string
- Status      Pending | InProgress | Completed
- CreatedAt   DateTimeOffset
- Version     uint
```

Thêm lịch sử trạng thái:

```text
WorkItemStatusHistory
- Id            Guid
- WorkItemId    Guid
- PreviousStatus
- NewStatus
- ChangedAt     DateTimeOffset
```

Quy tắc:

- Tạo WorkItem mới với trạng thái `Pending`.
- Title vẫn tuân theo rule của Lab 1.
- Mỗi lần đổi trạng thái hợp lệ phải lưu thêm một history record.
- WorkItem và history phải được ghi trong cùng transaction.
- Client phải gửi `Version` cuối cùng mà nó đã đọc khi cập nhật.
- Nếu dữ liệu đã bị request khác sửa, API trả `409 Conflict`.
- Dữ liệu phải còn sau khi API restart.

## 5. API contract

Giữ các endpoint cũ và bổ sung concurrency token:

| Method | Route | Kết quả |
|---|---|---|
| `GET` | `/api/v1/work-items` | Danh sách từ PostgreSQL |
| `GET` | `/api/v1/work-items/{id}` | Một item hoặc `404` |
| `POST` | `/api/v1/work-items` | Tạo mới, trả `201` |
| `PUT` | `/api/v1/work-items/{id}/status` | Cập nhật hoặc `404`/`409`/`422` |
| `GET` | `/api/v1/work-items/{id}/history` | Lịch sử trạng thái |

Request cập nhật phải mang ý nghĩa:

```json
{
  "status": "InProgress",
  "version": 1
}
```

Response của WorkItem phải trả lại `version` mới nhất để client dùng cho lần cập nhật tiếp theo.

## 6. Technical constraints

- .NET 8.
- EF Core 8.
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x.
- PostgreSQL thật; không dùng EF Core InMemory hoặc SQLite để kiểm tra `xmin`.
- Mọi truy vấn và ghi dữ liệu dùng API async và nhận `CancellationToken`.
- `DbContext`, repository, Unit of Work và application service dùng lifetime `Scoped`.
- Không inject `ApplicationDbContext` vào Presentation.
- Application không tham chiếu project Persistence.
- Không gọi `SaveChangesAsync` trong Carter endpoint.
- Không tự động gọi `EnsureCreated()` khi đã dùng migration.
- Không lưu password production vào source hoặc commit connection string bí mật.
- Chưa dùng MediatR trong lab này để tập trung hoàn toàn vào persistence.

## 7. Cấu trúc đích

Giữ cấu trúc Lab 1 và thêm các phần sau:

```text
src/
├── production_api_skeleton.API/
│   └── DependencyInjection/
├── production-api-skeleton.Presentation/
├── production-api-skeleton.Application/
│   └── WorkItems/
├── production-api-skeleton.Domain/
│   ├── Abstractions/
│   │   └── Repositories/
│   │       ├── IWorkItemRepository.cs
│   │       └── IUnitOfWork.cs
│   └── WorkItems/
│       ├── WorkItem.cs
│       ├── WorkItemStatus.cs
│       └── WorkItemStatusHistory.cs
├── production-api-skeleton.Contract/
└── production-api-skeleton.Persistence/
    ├── ApplicationDbContext.cs
    ├── ApplicationDbContextFactory.cs
    ├── AssemblyReference.cs
    ├── Configurations/
    │   ├── WorkItemConfiguration.cs
    │   └── WorkItemStatusHistoryConfiguration.cs
    ├── DependencyInjection/
    │   └── PersistenceServiceCollectionExtensions.cs
    ├── Repositories/
    │   ├── WorkItemRepository.cs
    │   └── EfUnitOfWork.cs
    ├── Seed/
    │   └── WorkItemSeed.cs
    └── Migrations/

tests/
└── production-api-skeleton.Api.IntegrationTests/
    ├── PostgreSqlContainerFixture.cs
    ├── CustomWebApplicationFactory.cs
    └── WorkItemPersistenceTests.cs
```

Dependency direction:

```text
API
├── Presentation
├── Application
└── Persistence          ← composition root kết nối implementation

Presentation
├── Application
└── Contract

Application
├── Domain
└── Contract

Persistence
└── Domain               ← implement abstraction của Domain

Domain                   → không tham chiếu project khác
Contract                 → không tham chiếu project khác
```

## 8. Khởi tạo Persistence project

Thực hiện trong repository mới đã copy từ Lab 1. Điều chỉnh tên solution nếu source của bạn khác ví dụ dưới đây.

```powershell
dotnet new classlib `
  -n production-api-skeleton.Persistence `
  -o src/production-api-skeleton.Persistence `
  --framework net8.0

dotnet sln add `
  src/production-api-skeleton.Persistence/production-api-skeleton.Persistence.csproj

dotnet add `
  src/production-api-skeleton.Persistence/production-api-skeleton.Persistence.csproj `
  reference src/production-api-skeleton.Domain/production-api-skeleton.Domain.csproj

dotnet add `
  src/production_api_skeleton.API/production_api_skeleton.API.csproj `
  reference src/production-api-skeleton.Persistence/production-api-skeleton.Persistence.csproj
```

Package cần cho Persistence:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
```

Package bổ sung cho integration test:

```xml
<PackageReference Include="Testcontainers.PostgreSql" Version="4.8.1" />
```

Nếu NuGet không có đúng patch version trên, dùng bản `4.x` mới nhất tương thích với `net8.0` và ghi version thực tế vào README.

Sau khi thêm project và package:

```powershell
dotnet restore
dotnet build
dotnet list reference
```

Không qua checkpoint tiếp theo nếu dependency direction bị ngược hoặc solution chưa build.

## 9. Checkpoint 1 — Tạo persistence boundary

Việc cần làm:

1. Xóa `ConcurrentDictionary` khỏi Application service.
2. Định nghĩa `IWorkItemRepository` và `IUnitOfWork` ở Domain abstractions.
3. Application chỉ sử dụng hai abstraction này.
4. Tạo project Persistence để implement chúng.
5. Đổi application service và interface sang async.
6. Truyền `CancellationToken` từ endpoint tới database call.

Behavior contract tối thiểu:

```text
IWorkItemRepository
- GetAllAsync(cancellationToken)
- GetByIdAsync(id, tracking, cancellationToken)
- AddAsync(workItem, cancellationToken)
- AddHistoryAsync(history, cancellationToken)
- GetHistoryAsync(workItemId, cancellationToken)

IUnitOfWork
- BeginTransactionAsync(cancellationToken)
- SaveChangesAsync(cancellationToken)
- CommitAsync(cancellationToken)
- RollbackAsync(cancellationToken)
```

Đây là behavior contract, không phải code bắt buộc phải sao chép nguyên dạng.

Điều kiện qua checkpoint:

- Application không import namespace EF Core.
- Persistence reference Domain, không có chiều ngược lại.
- Endpoint không biết `DbContext` hoặc repository cụ thể.
- Solution build thành công dù repository implementation chưa hoàn chỉnh.

## 10. Checkpoint 2 — ApplicationDbContext và entity configuration

Việc cần làm:

1. Tạo `ApplicationDbContext` với constructor nhận `DbContextOptions<ApplicationDbContext>`.
2. Khai báo `DbSet<WorkItem>` và `DbSet<WorkItemStatusHistory>`.
3. Trong `OnModelCreating`, dùng `ApplyConfigurationsFromAssembly`.
4. Tách mapping của từng entity sang `IEntityTypeConfiguration<TEntity>`.
5. Không đặt Fluent API mapping dài trực tiếp trong DbContext.

Mapping bắt buộc cho WorkItem:

- Table name rõ ràng.
- Primary key là `Id`.
- `Title` required và có max length bằng giới hạn đã chọn.
- `Status` lưu dưới dạng string có giới hạn độ dài.
- `CreatedAt` required.
- `Version` được cấu hình bằng `.IsRowVersion()` để Npgsql ánh xạ tới `xmin`.

Mapping bắt buộc cho history:

- Primary key là `Id`.
- Foreign key tới WorkItem.
- Index theo `WorkItemId` và `ChangedAt`.
- Delete behavior phải được chọn có chủ đích và ghi lý do trong README.

Điều kiện qua checkpoint:

- DbContext chỉ giữ DbSet và orchestration mapping.
- Mỗi entity có configuration riêng.
- Có thể giải thích convention nào EF tự suy ra và phần nào bạn cấu hình rõ ràng.

## 11. Checkpoint 3 — DI, lifetime và connection string

Tạo extension method có ý nghĩa tương tự:

```text
AddPostgreSqlPersistence(IServiceCollection, IConfiguration)
```

Extension chịu trách nhiệm:

- Đọc connection string từ configuration.
- Đăng ký `ApplicationDbContext` với `UseNpgsql`.
- Chỉ bật detailed errors/sensitive logging ở Development.
- Đăng ký repository và Unit of Work.

Lifetime bắt buộc:

```text
ApplicationDbContext  → Scoped
IWorkItemRepository   → Scoped
IUnitOfWork           → Scoped
WorkItem service      → Scoped
```

Không commit mật khẩu thật. Ưu tiên một trong hai cách:

```powershell
dotnet user-secrets set "ConnectionStrings:PostgresSql" "<local-connection-string>" `
  --project src/production_api_skeleton.API
```

hoặc biến môi trường:

```text
ConnectionStrings__PostgresSql=<local-connection-string>
```

Failure experiment:

- Cố tình bỏ connection string.
- Khởi động API.
- Xác nhận ứng dụng báo lỗi cấu hình dễ nhận biết, không đợi tới request đầu tiên mới lỗi mơ hồ.

## 12. Checkpoint 4 — Migration và seed data

Nếu chưa có EF CLI đúng major version:

```powershell
dotnet tool update --global dotnet-ef --version 8.*
```

Tạo migration:

```powershell
dotnet ef migrations add InitialWorkItemPersistence `
  --project src/production-api-skeleton.Persistence `
  --startup-project src/production_api_skeleton.API `
  --output-dir Migrations
```

Áp dụng migration:

```powershell
dotnet ef database update `
  --project src/production-api-skeleton.Persistence `
  --startup-project src/production_api_skeleton.API
```

Yêu cầu:

1. Đọc migration được sinh ra trước khi apply.
2. Kiểm tra table, foreign key, index và max length.
3. Kiểm tra `Version` được ánh xạ tới PostgreSQL `xmin`.
4. Seed tối thiểu một WorkItem với ID cố định.
5. Seed phải idempotent và không tạo dữ liệu trùng khi chạy lại.
6. Không sửa migration đã được xem như đã deploy; tạo migration mới cho thay đổi tiếp theo.

Không trộn `EnsureCreated()` với migrations.

## 13. Checkpoint 5 — Repository, tracking và query

Implement `WorkItemRepository` bằng EF Core.

Quy tắc:

- Query chỉ đọc dùng `AsNoTracking()`.
- Query chuẩn bị cập nhật phải tracking entity.
- Không gọi `SaveChangesAsync` bên trong từng method `Add` hoặc `Update` của repository.
- Unit of Work là nơi quyết định thời điểm lưu.
- Không trả `IQueryable` ra khỏi Persistence.
- Có sắp xếp rõ ràng cho `GetAll` và history.
- Mọi query async nhận `CancellationToken`.

Failure experiment:

1. Đọc entity bằng `AsNoTracking()`.
2. Thay đổi property nhưng không attach/update entity.
3. Gọi `SaveChangesAsync()`.
4. Xác nhận database không thay đổi và giải thích vì sao.

Câu hỏi cần trả lời:

- Khi nào dùng tracking, khi nào dùng `AsNoTracking()`?
- Vì sao `DbContext` không nên là Singleton?
- Repository có lợi gì và đang che mất khả năng nào của EF Core?

## 14. Checkpoint 6 — Transaction và Unit of Work

Use case đổi trạng thái phải:

1. Bắt đầu transaction.
2. Load WorkItem dạng tracking.
3. Kiểm tra version do client gửi.
4. Gọi domain method để đổi trạng thái.
5. Thêm `WorkItemStatusHistory`.
6. Save changes.
7. Commit khi toàn bộ thành công.
8. Rollback khi có exception hoặc cancellation.

Lưu ý:

- Một lần `SaveChangesAsync()` đã có tính atomic cho chính lần save đó.
- Explicit transaction chỉ có ý nghĩa rõ hơn khi use case có nhiều lần save hoặc nhiều bước database phải cùng thành công/thất bại.
- Không mở transaction ở Presentation.
- Không giữ transaction trong lúc gọi HTTP API ngoài hệ thống.

Failure experiment bắt buộc:

- Tạm ném exception sau lần ghi WorkItem nhưng trước khi hoàn thành history.
- Rollback transaction.
- Kiểm tra cả trạng thái WorkItem và history đều không bị ghi dở.

## 15. Checkpoint 7 — Optimistic concurrency bằng xmin

Cơ chế phải đạt được:

```text
Request A đọc WorkItem với Version = 10
Request B đọc WorkItem với Version = 10
Request A cập nhật thành công → PostgreSQL đổi xmin
Request B cập nhật bằng version cũ → EF Core phát hiện 0 row được update
→ DbUpdateConcurrencyException
→ API trả 409 Conflict
```

Yêu cầu:

1. Domain entity có property `uint Version`.
2. Configuration đánh dấu property bằng `.IsRowVersion()`.
3. Response trả version hiện tại.
4. Update request gửi expected version.
5. Application kiểm tra version và vẫn để database làm hàng rào cuối cùng.
6. Không tự động retry một command ghi nếu chưa xác định thao tác có an toàn để chạy lại.
7. Trả Problem Details với mã lỗi ổn định, ví dụ `WorkItem.ConcurrencyConflict`.

Không dùng `lock (item)` của Lab 1. Lock trong process không bảo vệ được khi chạy nhiều API instance.

Điều kiện qua checkpoint:

- Hai `DbContext` khác nhau cùng sửa một row thì chỉ một bên thành công.
- Bên thua nhận conflict, không âm thầm ghi đè dữ liệu mới.
- Client có đủ ID và version mới để đọc lại rồi quyết định thử lại.

## 16. Checkpoint 8 — Integration test với PostgreSQL thật

Dùng:

- `WebApplicationFactory<Program>` để chạy API trong test host.
- `PostgreSqlContainer` để tạo database tạm bằng Docker.
- `CustomWebApplicationFactory` để thay connection string của API bằng connection string container.
- Migration thật để tạo schema test.

Lifecycle mong muốn:

```text
Test collection bắt đầu
→ Start PostgreSQL container
→ Tạo WebApplicationFactory
→ Override DbContext configuration
→ Apply migrations
→ Chạy HTTP tests
→ Dispose factory
→ Stop container
```

Yêu cầu isolation:

- Test không phụ thuộc thứ tự chạy.
- Mỗi test dùng ID riêng hoặc reset database có kiểm soát.
- Không gọi database local/dev từ integration test.
- Không dùng EF Core InMemory để thay cho PostgreSQL.

## 17. Required tests

Tối thiểu phải có:

1. `POST` hợp lệ trả `201` và row tồn tại trong PostgreSQL.
2. Tạo xong, dùng scope/DbContext mới vẫn đọc được dữ liệu.
3. `GET` dùng ID không tồn tại trả `404` Problem Details.
4. Update hợp lệ thay đổi status và tạo đúng một history record.
5. Transaction gặp lỗi giữa chừng không để lại dữ liệu ghi dở.
6. Hai DbContext đọc cùng một version; lần save thứ hai ném concurrency exception.
7. HTTP update với stale version trả `409` Problem Details.
8. Response sau update trả version mới khác version cũ.
9. Restart test host nhưng dùng cùng database vẫn đọc được dữ liệu.
10. Cancellation token được truyền xuống EF Core operation.

Test quan trọng nhất của lab là test concurrency với hai DbContext độc lập. Nếu chỉ gọi tuần tự bằng cùng một DbContext thì chưa chứng minh được optimistic concurrency.

## 18. Definition of done

Lab hoàn thành khi:

- `dotnet build` thành công.
- `dotnet test` thành công khi Docker đang chạy.
- Không còn in-memory collection.
- Dữ liệu tồn tại sau khi API restart.
- Có project Persistence riêng.
- Application không tham chiếu EF Core/Persistence.
- DbContext và mapping không nằm trong API.
- Migration tạo đúng table, FK, index và row version.
- Query đọc dùng `AsNoTracking()` có chủ đích.
- Transaction không để lại partial write.
- Stale update trả `409` và không ghi đè dữ liệu.
- Integration test chạy với PostgreSQL thật trong container.
- Không có secret thật trong Git.
- README mô tả request flow, transaction boundary và concurrency flow.

## 19. Proof of understanding

Trước khi yêu cầu review, tự trả lời trong README:

1. `DbContext` vừa là Unit of Work vừa có behavior giống repository ở điểm nào?
2. Tại sao project vẫn tạo repository abstraction thay vì inject DbContext trực tiếp vào Application?
3. Vì sao DbContext phải là Scoped, không phải Singleton?
4. Tracking và `AsNoTracking()` khác nhau thế nào?
5. Vì sao không gọi `SaveChangesAsync()` trong từng repository method?
6. Một lần `SaveChangesAsync()` có transaction ngầm ra sao?
7. Khi nào cần explicit transaction?
8. PostgreSQL `xmin` thay đổi khi nào?
9. Vì sao `lock` trong C# không thay thế optimistic concurrency?
10. Vì sao không được dùng EF Core InMemory để test behavior riêng của PostgreSQL?
11. Khi nhận `DbUpdateConcurrencyException`, tại sao không luôn retry tự động?
12. Migration khác `EnsureCreated()` như thế nào trong vòng đời production?

## 20. So sánh lại với production

Sau khi test pass, đối chiếu:

1. `ApplicationDbContext` của lab với `bmt-be.persistence/ApplicationDbContext.cs`.
2. DI extension của lab với `AddPostgresSqlPersistence`.
3. Entity configuration của lab với một configuration nhỏ trong `bmt-be.persistence/configurations`.
4. Unit of Work của lab với `bmt-be.persistence/repositories/EFUnitOfWork.cs`.
5. Mapping `.IsRowVersion()` của lab với `UserConfiguration` hoặc `AiPromptTrialConfiguration`.
6. Cách lab trả `409` với mapping `DbUpdateConcurrencyException` trong production exception middleware.

Khi so sánh, phân biệt rõ:

- Kỹ thuật có thể tái sử dụng.
- Convention riêng của project.
- Complexity chỉ cần thiết vì production có nhiều nghiệp vụ hơn.
- Điểm lab cố tình làm nhỏ để học một cơ chế.

## 21. Non-goals

Chưa làm trong lab này:

- MediatR/CQRS pipeline.
- Authentication và authorization.
- Redis hoặc distributed lock.
- Background job và message broker.
- Outbox/inbox.
- SignalR.
- Soft delete, audit interceptor tổng quát hoặc multi-tenancy.
- Generic repository cho mọi entity.
- Deploy database lên môi trường thật.

Không thêm các kỹ thuật trên chỉ để project trông lớn hơn.

## 22. Cách làm việc với mentor

Thực hiện từng checkpoint theo thứ tự. Khi gặp phần chưa hiểu, gửi:

```text
Lab 2 - Checkpoint <số>
Tôi đang làm: <việc đang làm>
Tôi chưa hiểu: <khái niệm hoặc đoạn code>
Code/error hiện tại: <đường dẫn hoặc lỗi>
```

Sau mỗi checkpoint:

1. Build hoặc chạy test liên quan.
2. Giải thích lại cơ chế bằng lời của bạn.
3. Chỉ chuyển tiếp khi kết quả đã được xác minh.

Không cần xin toàn bộ lời giải ngay từ đầu. Ưu tiên hỏi về dependency direction, DbContext lifetime, transaction boundary, tracking và concurrency failure mode.
