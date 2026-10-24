using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using DotNetEfCorePostgreSqlLab.Domain.Abstractions.Repositories;
using DotNetEfCorePostgreSqlLab.Domain.Exceptions;

namespace DotNetEfCorePostgreSqlLab.Persistence.Repositories;

public class EfUnitOfWork: IUnitOfWork, IAsyncDisposable
{
    private readonly AppDbContext _dbContext;
    private IDbContextTransaction? _transaction;
    //IDbContextTransaction là môt object đại diện cho một database transaction đang mở của EF Core.
    public EfUnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A database transaction is already active");
        }
        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        //EF yêu cầu Postgres mở transaction và trả về mt object để chúng ta điều khiển transaction đó 
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try 
        { 
            return await _dbContext.SaveChangesAsync( 
                cancellationToken); 
        } 
        catch (DbUpdateConcurrencyException exception) 
        { 
            throw new ConcurrencyConflictException("The data was changed by another request.", exception); 
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null) 
        { 
            throw new InvalidOperationException("No active database transaction exists."); 
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
        
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null) 
        { 
            return; 
        }
        
        try 
        { 
            await _transaction.RollbackAsync( 
                cancellationToken); 
        } 
        finally 
        { 
            await DisposeTransactionAsync(); 
        }
        
    }
    //thằng này thì DI se gọi để dọn dẹp toàn bộ EfUniOfWork khi object kết thúc vòng đời
    public async ValueTask DisposeAsync()
    {
        // TODO release managed resources here
        if(_transaction is not null) 
        { 
            await RollbackAsync(CancellationToken.None); 
        }
    }
    //sau khi commit hoặc rollback thì thằng object transaction này nó vẫn giữ tài nguyên
    //=> DisposeTransactionAsync sinh ra để dọn dẹp và giải phóng những tài nguyên đó 
    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null)
        {
            return;
        }
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
