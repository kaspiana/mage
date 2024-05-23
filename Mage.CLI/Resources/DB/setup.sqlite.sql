
---
--- DOCUMENTS
---

create table document (
    id              integer not null primary key autoincrement,
    hash            text not null,
    file_name       text not null,
    file_ext        text not null,
    file_size       integer not null, -- bytes
    ingested_at     integer not null, -- unix timestamp
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