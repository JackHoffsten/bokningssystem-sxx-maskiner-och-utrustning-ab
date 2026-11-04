# UC-02 – Registrera återlämning

## 1. Översikt

**Användningsfall:** UC-02 – Registrera återlämning  
**Primär aktör:** Personal

**Syfte:** Registrera att uthyrd utrustning har återlämnats, dokumentera vilken anställd som tog emot utrustningen och säkerställa att bokningens överenskomna totalpris bevaras.

**Relaterade User Stories:**
- US-37 – Registrera återlämning
- US-38 – Registrera mottagande anställd
- US-39 – Bevara pris vid tidig återlämning

**Kravkoppling:**
- FR-22 – Registrera återlämning.
- FR-23 – Registrera vilken anställd som genomförde återlämningen.
- BR-12 – Tidig återlämning ändrar inte automatiskt priset.
- K08 – Utlämning och återlämning.
- K12 – Överenskommet pris.

## 2. Förutsättningar

- Personalen är inloggad i systemet.
- Bokningen finns registrerad i systemet.
- Utrustningen har tidigare lämnats ut.
- Bokningen har status `CheckedOut`, vilket innebär att uthyrningen är påbörjad.

## 3. Huvudflöde

1. Personalen söker efter uthyrningen med bokningsnummer.
2. Systemet hämtar bokningen och visar bokningsuppgifterna.
3. Systemet kontrollerar att bokningen har status `CheckedOut`.
4. Personalen bekräftar att utrustningen har återlämnats.
5. Systemet identifierar den inloggade anställda.
6. Systemet förbereder en återlämningshändelse med boknings-ID, ansvarig anställd och återlämningstid.
7. Systemet säkerställer att bokningens sparade totalpris (`AgreedTotalPrice`) förblir oförändrat.
8. Systemet kontrollerar att uthyrningen fortfarande är påbörjad och sparar återlämningshändelsen tillsammans med statusändringen till `Returned`.
9. Systemet visar en bekräftelse på att återlämningen har registrerats.
10. Återlämningshändelsen finns sparad och kan visas i efterhand.

## 4. Alternativa flöden

### A1 – Bokningen hittas inte (steg 2)

1. Systemet hittar ingen bokning med det angivna bokningsnumret.
2. Systemet visar ett felmeddelande.
3. Ingen återlämning registreras.
4. Användningsfallet avslutas.

### A2 – Uthyrningen är inte påbörjad (steg 3)

1. Systemet upptäcker att bokningen inte har status `CheckedOut`.
2. Systemet meddelar att uthyrningen inte kan registreras som återlämnad.
3. Ingen återlämning registreras.
4. Användningsfallet avslutas.

### A3 – Bokningens status har ändrats (steg 8)

1. Systemet upptäcker att bokningen inte längre har status `CheckedOut`.
2. Systemet avbryter registreringen.
3. Systemet visar ett felmeddelande.
4. Ingen ny återlämningshändelse sparas.
5. Användningsfallet avslutas.

Detta förhindrar exempelvis att samma uthyrning registreras som återlämnad två gånger vid samtidiga registreringsförsök.

### A4 – Registreringen misslyckas (steg 8)

1. Ett fel uppstår när återlämningen ska sparas.
2. Systemet avbryter registreringen.
3. Varken återlämningshändelsen eller statusändringen sparas.
4. Systemet visar ett felmeddelande.
5. Användningsfallet avslutas.

### A5 – Tidig återlämning (steg 7)

1. Utrustningen återlämnas före bokningens planerade slutdatum.
2. Systemet behåller bokningens sparade totalpris oförändrat.
3. Återlämningen fortsätter enligt huvudflödet från steg 8.

## 5. Resultat efter genomförandet

**Vid lyckad återlämning:**
- Återlämningshändelsen har sparats på rätt bokning.
- Bokningens status har ändrats från `CheckedOut` till `Returned`.
- Den mottagande anställdas ID har registrerats.
- Återlämningstiden har dokumenterats.
- Bokningens överenskomna totalpris är oförändrat.
- Återlämningshändelsen kan hämtas och visas i efterhand.

**Vid misslyckad återlämning:**
- Ingen ny återlämningshändelse har sparats.
- Bokningens tidigare status är oförändrad.
- Bokningens sparade totalpris är oförändrat.

## 6. Koppling till UML-diagram

**Aktivitetsdiagram:** `docs/uml/US-37/US-37-aktivitetsdiagram.puml`

Visar återlämningsprocessens aktiviteter, beslut och alternativa flöden, inklusive kontroll av bokningsstatus och felhantering.

**Sekvensdiagram:** `docs/uml/US-37/US-37-sekvensdiagram.puml`

Visar kommunikationen mellan personal, bokningssystem, `ReturnService` och databasen vid registrering av återlämning.

**Klassdiagram:** `docs/uml/klassdiagram.puml`

Visar relevanta klasser och metoder för återlämningsprocessen, bland annat:

- `Booking` – innehåller bokningsinformation, status och överenskommet totalpris.
- `Return` – representerar återlämningshändelsen.
- `Personal` – representerar den anställda som tar emot utrustningen.
- `ReturnService` – hanterar registreringen av återlämningen.
- `BookingRepository` – ansvarar för hämtning och lagring av information.

## 7. Designbeslut och antaganden

### 7.1 Verksamhetsregler och krav

Följande regler ligger till grund för användningsfallet:

- Systemet ska kunna registrera återlämning av utrustning enligt FR-22.
- Ansvarig anställd ska registreras vid återlämning enligt FR-23.
- Tidig återlämning får inte automatiskt ändra det överenskomna priset enligt BR-12.
- Återlämningen ska kunna kopplas till rätt bokning och följas upp i efterhand.

### 7.2 Föreslagna tekniska designbeslut

För att stödja kraven har följande lösningar föreslagits:

**Separat återlämningstjänst**

`ReturnService` ansvarar för att hantera återlämningsprocessen och kontrollera att bokningen är giltig för återlämning.

**Separat återlämningsmodell**

En `Return`-klass används för att lagra boknings-ID, mottagande personal-ID och återlämningstid. Detta gör återlämningshändelsen spårbar.

**Identifiering av ansvarig personal**

Personalens ID hämtas från den inloggade användaren i stället för att anges manuellt. Detta minskar risken att fel anställd registreras som ansvarig.

**Kontroll av bokningsstatus**

Endast bokningar med status `CheckedOut` får registreras som återlämnade. Statusen kontrolleras även i samband med lagringen för att förhindra dubbla registreringar.

**Säker lagring**

Återlämningshändelsen och bokningens statusändring till `Returned` sparas i samma transaktion. Om registreringen misslyckas ska inga av ändringarna sparas.

**Bevarande av överenskommet pris**

`Booking.AgreedTotalPrice` ändras inte när återlämningen registreras, även om utrustningen återlämnas före det planerade slutdatumet.

### 7.3 Antaganden och öppna frågor

- Det antas att återlämningen hanteras av inloggad personal.
- Det antas att bokningens status ändras till `Returned` när återlämningen registreras.
- Det är ännu inte fastställt om återlämnad utrustning automatiskt ska bli bokningsbar igen.
- Det är ännu inte fastställt om utrustningens skick ska dokumenteras vid återlämning.

De tekniska lösningarna är designförslag som ska granskas av gruppen. Uppgiften omfattar analys och design, inte en fullständig implementation.