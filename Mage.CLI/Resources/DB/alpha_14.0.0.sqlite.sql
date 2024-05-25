create trigger trigger_populuate_document_rating_on_new_ranking
after insert on ranking
for each row begin
    insert into document_rating (
        document_id, 
        ranking_name
    )
    select
        document.id document_id,
        new.name ranking_name
    from document;
end;

create trigger trigger_populate_document_rating_on_new_document
after insert on document
for each row begin
    insert into document_rating (
        document_id,
        ranking_name
    )
    select
        new.id document_id,
        ranking.name ranking_name
    from ranking;
end;

insert or ignore into document_rating
select
    document.id document_id,
    ranking.name ranking_name,
    0
from
    document cross join ranking;