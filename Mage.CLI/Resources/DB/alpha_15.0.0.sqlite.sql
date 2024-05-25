create view ranking_statistics as
select
    ranking_name name,
    min(rating) min_rating,
    avg(rating) avg_rating,
    max(rating) max_rating
from
    document_rating
group by ranking_name;

create view document_rating_normalised as
select
    document_rating.document_id,
    document_rating.ranking_name,
    case 
        when (((rating - avg_rating) / abs(max_rating - avg_rating)) < 0
            or max_rating - avg_rating = 0)
	    then 0
	    else ((rating - avg_rating) / abs(max_rating - avg_rating)) 
    end rating_normalised
from
    document_rating
    inner join
    ranking_statistics
    on document_rating.ranking_name = ranking_statistics.name;