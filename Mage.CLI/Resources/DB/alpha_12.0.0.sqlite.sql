create table ranking (
    name            text not null primary key
);

create table document_ranking (
    document_id     integer not null,
    ranking_name    text not null,
    score           integer not null default 0,

    foreign key (document_id) references document(id),
    foreign key (ranking_name) references ranking(name),
    primary key (document_id, ranking_name)
);