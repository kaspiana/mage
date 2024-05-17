create table Document (
    ID integer not null primary key autoincrement,
    Hash text not null,
    FileName text not null,
    Extension text not null,
    IngestTimestamp integer not null,
    Comment text
);

create table TaxonymAlias (
	TaxonymID integer not null,
	Alias text not null,

	foreign key (TaxonymID) references Taxonym(ID)
		deferrable initially deferred,
	primary key (TaxonymID, Alias)
);

create table TaxonymParent (
	ChildID integer not null,
	ParentID integer not null,

	foreign key (ChildID) references Taxonym(ID)
        deferrable initially deferred,
	foreign key (ParentID) references Taxonym(ID)
        deferrable initially deferred,
	primary key (ChildID, ParentID)
);

create table Taxonym (
	ID integer not null primary key autoincrement,
	CanonicalParentID integer,
	CanonicalAlias text not null,

	foreign key (CanonicalParentID) references Taxonym(ID),
	foreign key (ID, CanonicalAlias) references TaxonymAlias(TaxonymID, Alias)
		deferrable initially deferred,
	foreign key (ID, CanonicalParentID) references TaxonymParent(ChildID, ParentID)
		deferrable initially deferred,
	unique(CanonicalParentID, CanonicalAlias)
);

begin transaction;
insert into TaxonymAlias (TaxonymID, Alias) values (1, "");
insert into Taxonym (CanonicalParentID, CanonicalAlias) values (null, ""); -- root taxonym
commit;





create table Tag (
	ID integer not null primary key autoincrement,
	TaxonymID integer not null,

	foreign key (TaxonymID) references Taxonym(ID),
	unique (TaxonymID)
);

create table DocumentTag (
	DocumentID integer not null,
   TagID integer not null,
	
	foreign key (DocumentID) references Document(ID),
	foreign key (TagID) references Tag(ID),
	primary key (DocumentID, TagID)
);

create table TagParameterisation (
	TagID integer not null primary key,

	foreign key (TagID) references Tag(ID)
);

create table DocumentTagParameter (
	DocumentID integer not null,
   TagID integer not null,
	Value integer not null,

	foreign key (DocumentID, TagID) references DocumentTag(DocumentID, TagID),
	foreign key (TagID) references TagParameterisation(TagID),
	primary key (DocumentID, TagID)
);



create table TagImplication (
	AntecedentID integer not null,
	ConsequentID integer not null,
	
	foreign key (AntecedentID) references Tag(ID),
	foreign key (ConsequentID) references Tag(ID),
	primary key (AntecedentID, ConsequentID)
);

create table Series (
	ID integer not null primary key autoincrement,
	Title text,
	Comment text
);

create table SeriesDocument (
	SeriesID integer not null,
	DocumentID integer not null,

	foreign key (SeriesID) references Series(ID),
	foreign key (DocumentID) references Document(ID),
	primary key (SeriesID, DocumentID)
);

create table Source (
	ID integer not null primary key autoincrement,
	URL text not null
);

create table DocumentSource (
	DocumentID integer not null,
	SourceID integer not null, 
	
	foreign key (DocumentID) references Document(ID),
	foreign key (SourceID) references Source(ID),
	primary key (SourceID, DocumentID)
);