Dorađujemo modele za implementaciju... tj stavke modela.

Dodajemo na stavku modela ček listu...
Kreirao sam dve nove tabele:

/****** Object:  Table [dbo].[CheckListItems]    Script Date: 15.2.2026. 18:25:34 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[CheckListItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Opis] [nvarchar](150) NOT NULL,
 CONSTRAINT [PK_CheckListItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


i još jednu:

/****** Object:  Table [dbo].[ImplementationItemCheckListItems]    Script Date: 15.2.2026. 18:29:48 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ImplementationItemCheckListItems](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ImplementationItemId] [int] NOT NULL,
	[CheckListItemId] [int] NOT NULL,
 CONSTRAINT [PK_ImplementationItemCheckListItems] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


Prva tabela je kao šifarnik za stavke ček liste i nećemo ni praviti formu za unos i ažuriranje za sada u aplikaciji... uneo sam neke vrednosti kroz bazu i dodaću kasnije šta mi treba.

Druga tabela služi da se na konkrtenu stavku modela implementacije dodaju stavke ček liste. Ovako će korisnik kreirati za svaku stavku implementacionog modela određene stavke ček liste koje su tim delom modela definisane. Moguće je na različite stavke modela dodati istu stavku ček liste iz tabele ImplementationItemCheckListItems

Treba nam izmena na formi za ažuriranje stavke modela u donjem delu jedan grid koji prikazuje pridružene stavke ček liste (ovo iz tabele ImplementationItemCheckListItems - to je vezna tabela), kao i alat za dodavanje novih stavki ček liste iz šifarnika (iz tabele ImplementationItemCheckListItems). Najbolje na bude modal prozor gde će biti u gridu prikazane sve iz tabele ImplementationItemCheckListItems, sa čekboxom za izbor i sa dugmetom za insert izabranih stavki. Na gridu pridruženih stavki dodaj dugme za brisanje (mala crvena ikonica kantice, ne crveno danger dugme)

Nemoj da se zbuniš - za sada ne radimo ništa sa modelima implementacije na projektima. To ćemo u drugom koraku. Sada menjamo samo osnovne šifarnike modela za implementaciju.