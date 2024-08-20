using LiveDocs.GraphQLApi.Data;
using LiveDocs.GraphQLApi.Models.Shared;
using Microsoft.EntityFrameworkCore;
using RxDBDotNet.Repositories;
using RxDBDotNet.Services;

namespace LiveDocs.GraphQLApi.Services;

public class HeroService : IDocumentService<Hero>
{
    private readonly LiveDocsDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly List<Hero> _pendingEvents = [];

    public HeroService(LiveDocsDbContext dbContext, IEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    public IQueryable<Hero> GetQueryableDocuments()
    {
        return _dbContext.Heroes.AsNoTracking();
    }

    public Task<List<Hero>> ExecuteQueryAsync(IQueryable<Hero> query, CancellationToken cancellationToken)
    {
        return query.ToListAsync(cancellationToken);
    }

    public Task<Hero?> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Heroes.Where(d => d.Id == id).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Hero> CreateDocumentAsync(Hero document, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Hero>()
            .AddAsync(document, cancellationToken);

        _pendingEvents.Add(document);

        return document;
    }

    public async Task<Hero> UpdateDocumentAsync(Hero document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var heroToUpdate = await GetDocumentByIdAsync(document.Id, cancellationToken)
                             ?? throw new InvalidOperationException($"Entity with a ReplicateDocumentId of {document.Id} not found for update.");

        heroToUpdate.Name = document.Name;
        heroToUpdate.Color = document.Color;
        heroToUpdate.UpdatedAt = DateTimeOffset.Now;

        _pendingEvents.Add(heroToUpdate);

        return heroToUpdate;
    }

    public async Task<Hero> MarkAsDeletedAsync(Hero document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        var heroToDelete = await GetDocumentByIdAsync(document.Id, cancellationToken)
                           ?? throw new InvalidOperationException($"Entity with a ReplicateDocumentId of {document.Id} not found for update.");

        heroToDelete.IsDeleted = true;
        heroToDelete.UpdatedAt = DateTimeOffset.Now;

        _pendingEvents.Add(heroToDelete);

        return heroToDelete;
    }

    public bool AreDocumentsEqual(Hero document1, Hero document2)
    {
        return true;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var doc in _pendingEvents)
        {
            await _eventPublisher.PublishDocumentChangedEventAsync(doc, cancellationToken);
        }

        _pendingEvents.Clear();
    }
}
