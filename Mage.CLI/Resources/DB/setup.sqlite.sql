
---
--- DOCUMENTS
---

create table document (
    id              integer not null primary key autoincrement,
    hash            text not null,
    file_name       text not null,
    file_ext        text not null,
    file_size       integer not null, -- bytes
    media_type      text check(media_type in ('b', 't', 'i', 'm', 'a', 'v')) not null default 'b',
                    -- b = binary
                    -- t = text
                    -- i = image
                    -- m = animation
                    -- a = audio
                    -- v = video
    added_at        integer not null default (unixepoch()), -- unix timestamp
    updated_at      integer not null default (unixepoch()), -- unix timestamp
    comment         text,
    is_deleted      integer not null default 0
);

create view public_document as 
    select * 
    from document 
    where is_deleted = 0;

create view deleted_document as 
    select * 
    from document 
    where is_deleted = 1;

-- document.media_type = 'i'
create table image_metadata (
    document_id     integer not null primary key,

    width           integer not null,
    height          integer not null,

    foreign key (document_id) references document(id)
);

-- document.media_type = 'a'
create table audio_metadata (
    document_id     integer not null primary key,

    duration        integer not null, -- milliseconds

    foreign key (document_id) references document(id)
);

-- document.media_type = 'm' or 'v'
create table video_metadata (
    document_id     integer not null primary key,

    width           integer not null,
    height          integer not null,
    duration        integer not null, -- milliseconds

    foreign key (document_id) references document(id)
);

---
--- TAXONYMS
---

create table taxonym (
	id                  integer not null primary key autoincrement,
	canon_parent_id     integer,
	canon_alias         text not null,

	foreign key (canon_parent_id) references taxonym(id),
	foreign key (id, canon_alias) references taxonym_alias(taxonym_id, alias) deferrable initially deferred,
	foreign key (id, canon_parent_id) references taxonym_parent(child_id, parent_id) deferrable initially deferred,
	unique(canon_parent_id, canon_alias)
);

create table taxonym_alias (
	taxonym_id  integer not null,
	alias       text not null,

	foreign key (taxonym_id) references taxonym(id) deferrable initially deferred,
	primary key (taxonym_id, alias)
);

create table taxonym_parent (
	child_id    integer not null,
	parent_id   integer not null,

	foreign key (child_id) references taxonym(id) deferrable initially deferred,
	foreign key (parent_id) references taxonym(id) deferrable initially deferred,
	primary key (child_id, parent_id)
);

begin transaction;
insert into taxonym_alias (taxonym_id, alias) values (1, "");
insert into taxonym (canon_parent_id, canon_alias) values (null, ""); -- root taxonym
commit;

---
--- TAGS
---

create table tag (
	id          integer not null primary key autoincrement,
	taxonym_id  integer not null,

	foreign key (taxonym_id) references taxonym(id),
	unique (taxonym_id)
);

create table tag_parameter (
	tag_id integer not null primary key,

	foreign key (tag_id) references tag(id)
);

create table tag_implication (
	antecedent_id integer not null,
	consequent_id integer not null,
	
	foreign key (antecedent_id) references tag(id),
	foreign key (consequent_id) references tag(id),
	primary key (antecedent_id, consequent_id)
);

create table document_tag (
	document_id     integer not null,
    tag_id          integer not null,
	
	foreign key (document_id) references document(id),
	foreign key (tag_id) references tag(id),
	primary key (document_id, tag_id)
);

create table document_tag_parameter (
	document_id     integer not null,
    tag_id          integer not null,
	value           integer not null,

	foreign key (document_id, tag_id) references document_tag(document_id, tag_id),
	foreign key (tag_id) references tag_parameter(tag_id),
	primary key (document_id, tag_id)
);

create view public_document_tag as
    select document_tag.*
    from 
        document_tag inner join document
        on document.id = document_tag.document_id
    where document.is_deleted = 0;

---
--- COLLECTIONS
---

create table collection (
	id          integer not null primary key autoincrement,
	title       text,
	comment     text
);

create table collection_document (
	collection_id   integer not null,
	document_id     integer not null,

	foreign key (collection_id) references collection(id),
	foreign key (document_id) references document(id),
	primary key (collection_id, document_id)
);

create view collection_public_document as
    select collection_document.*
    from
        collection_document inner join document
        on document.id = collection_document.document_id
    where document.is_deleted = 0;

---
--- SOURCES
---

create table document_source (
	document_id     integer not null,
	url             text not null, 
	
	foreign key (document_id) references document(id),
	primary key (document_id, url)
);

create view public_document_source as
    select document_source.*
    from 
        document_source inner join document
        on document.id = document_source.document_id
    where document.is_deleted = 0;

---
--- RANKING
---

create table ranking (
    name            text not null primary key
);

create table document_rating (
    document_id     integer not null,
    ranking_name    text not null,
    rating          integer not null default 0,

    foreign key (document_id) references document(id),
    foreign key (ranking_name) references ranking(name),
    primary key (document_id, ranking_name)
);

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