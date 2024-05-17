create table Document (
    ID integer not null primary key autoincrement,
    Hash text not null,
    FileName text not null,
    Extension text not null,
    IngestTimestamp integer not null,
    Comment text
);