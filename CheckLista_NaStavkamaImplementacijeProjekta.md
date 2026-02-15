Proširujemo funkcionalnost praćenja implemantacije na projektima.

Na stavci implementacije na projektu (ProjectImplementationItems) dodeljujemo stavke CheckListe koje su definisane u izabranom modelu.

Već postoji funkcionalnost kreiranja stavki implementacije za konkretan projekat prilikom izbora modela implemantacje na projektu, sada dodajemo i kreiranje stavki check liste sa svaku krieranu stavku implementacije u istom tom tom trenutku.

Treba kreirati novu tabelu gde ćemo pisati vezi izmedju stavke implementacije projekta i Id stavke check liste (iz tabele ImplementationItemCheckListItems). U ovoj tabeli predvidi i bit polje "Završeno".

Prikaz podataka iz ove tabele obezbediti na formi za ažuriranje stavke implementacije (grid u donjem delu).

Takođe, dodati kontrolu da na stavki implementacije ne može da se kliken na Završeno i potvrđeno od strane klijenta dok sve stavke ček liste ne budu setovane na Zavrseno = true.