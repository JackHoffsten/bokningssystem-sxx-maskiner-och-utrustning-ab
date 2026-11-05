# UC-01 – Boka utrustning

## 1. Översikt

**Användningsfall:** UC-01 – Boka utrustning  
**Primär aktör:** Kund  
**Sekundär aktör:** Personal (vid bokning åt kund)

**Syfte:** Göra det möjligt för en inloggad kund att boka ett tillgängligt exemplar av en utrustningsmodell under en sammanhängande period. Systemet ska kontrollera tillgänglighet, visa totalpris före bekräftelse, förhindra dubbelbokning och skapa en bekräftad bokning med ett unikt bokningsnummer.

**Relaterade User Stories:**

- US-16 – Skapa bokning
- US-17 – Tilldela exemplar utan inventarieval
- US-18 – Reservera vid bekräftelse
- US-19 – Få bokningsnummer
- US-20 – Se bokningsbekräftelse
- US-21 – Se totalpris före bekräftelse
- US-22 – Bevara överenskommet pris
- US-23 – Få tydligt besked vid nekad bokning

**Kravkoppling:**

- **K02:** Välja utrustningsmodell och period samt kontrollera tillgänglighet.
- **K03:** Kunden ska vara inloggad för att boka.
- **K04:** Boka ett exemplar, få bokningsnummer och se bokningen direkt.
- **K07:** Personal kan boka åt kunder enligt samma tillgänglighetsregler.
- **K09:** Otillgängliga exemplar får inte användas för nya bokningar.
- **K12:** Visa totalpris före bekräftelse och bevara överenskommet pris.

**Relevanta verksamhetsregler:**

- Bokningar görs i hela kalenderdagar; både första och sista dagen räknas.
- Ett exemplar bokat till och med fredag kan tidigast bokas av en annan kund från måndag.
- Varje bokning gäller ett fysiskt exemplar av en modell; flera saker kräver separata bokningar.
- Bokningens första dag får inte ligga bakåt i tiden.
- Exemplaret reserveras vid bekräftelse.
- Samma exemplar får inte ha överlappande aktiva bokningar, inte heller vid samtidiga bokningsförsök.
- Exemplar markerade som otillgängliga får inte användas för nya bokningar.

## 2. Förutsättningar

- Kunden har ett registrerat konto och är inloggad.
- Den valda utrustningsmodellen ingår i aktuellt sortiment och har ett dagspris.
- Systemet kan hämta modellens fysiska exemplar och befintliga bokningar.
- Systemet kan kontrollera tillgänglighet enligt verksamhetsreglerna.

## 3. Huvudflöde

1. Kunden öppnar utrustningskatalogen.
2. Systemet visar modeller med namn, beskrivning, kategori och dagspris.
3. Kunden väljer en utrustningsmodell.
4. Kunden anger startdatum och slutdatum.
5. Systemet validerar perioden: startdatum får inte ligga bakåt i tiden och slutdatum får inte föregå startdatum.
6. Systemet hämtar modellens exemplar och befintliga bokningar.
7. Systemet kontrollerar om minst ett exemplar är bokningsbart under hela perioden, med hänsyn till befintliga bokningar, otillgänglighet och övriga bokningsregler.
8. Systemet beräknar antalet hyresdagar inklusive första och sista dagen.
9. Systemet beräknar totalpriset utifrån aktuellt dagspris och antalet hyresdagar.
10. Systemet visar vald modell, bokningsperiod och totalpris.
11. Kunden granskar uppgifterna och bekräftar bokningen.
12. Systemet kontrollerar kundens identitet och validerar bokningsuppgifterna på nytt.
13. Systemet väljer ett bokningsbart exemplar utan att kunden behöver ange inventarienummer.
14. Systemet förbereder en bokning med kund, exemplar, period, status `Confirmed`, överenskommet totalpris (`AgreedTotalPrice`) och unikt bokningsnummer.
15. `BookingService` begär slutkontroll och sparande via `BookingRepository.TrySaveBooking`.
16. Systemet kontrollerar och sparar bokningen i en databasskyddad operation som förhindrar dubbelbokning vid samtidiga försök.
17. När sparandet lyckas är bokningen registrerad och exemplaret reserverat för perioden.
18. Systemet visar bokningsbekräftelse med bokningsnummer, modell, period och överenskommet totalpris.
19. Kunden kan se bokningen bland sina egna bokningar.

## 4. Alternativa flöden

### A1 – Kunden är inte inloggad (steg 12)

1. Systemet upptäcker att kunden inte är autentiserad.
2. Bokningsförsöket avvisas och kunden informeras om att inloggning krävs.
3. Ingen bokning skapas.
4. Kunden kan logga in och försöka igen.

### A2 – Ogiltig bokningsperiod (steg 5 eller 12)

1. Startdatum ligger bakåt i tiden eller slutdatum föregår startdatum.
2. Systemet visar ett begripligt felmeddelande.
3. Kunden får ändra datumen och fortsätta från steg 4.
4. Ingen bokning skapas innan perioden är giltig.

### A3 – Inget bokningsbart exemplar finns (steg 7)

1. Systemet hittar inget exemplar som är bokningsbart under hela perioden.
2. Kunden informeras om att modellen inte är tillgänglig för den valda perioden.
3. Kunden kan välja en annan modell eller period.
4. Ingen bokning skapas.

### A4 – Exemplaret hinner bli upptaget (steg 15–16)

1. Ett exemplar som tidigare var ledigt bokas av någon annan innan den aktuella bokningen sparas.
2. Systemet gör en slutlig tillgänglighetskontroll vid sparandet.
3. Om det valda exemplaret inte längre är bokningsbart avvisas sparandet.
4. Kunden får ett begripligt meddelande om tillgänglighetskonflikten och kan försöka igen.
5. Ingen ny bokning sparas och ingen dubbelbokning uppstår.

### A5 – Exemplar är markerat som otillgängligt (steg 7 eller 16)

1. Systemet upptäcker att ett exemplar är otillgängligt på grund av exempelvis skada eller service.
2. Exemplaret utesluts från nya bokningar.
3. Om ett annat bokningsbart exemplar av samma modell finns kan flödet fortsätta.
4. Annars meddelas kunden att utrustningen inte är tillgänglig; ingen bokning skapas.

### A6 – Kunden avbryter före bekräftelse (steg 10–11)

1. Kunden avbryter bokningsförsöket.
2. Ingen bokning skapas och inget exemplar reserveras.

### A7 – Samtidiga bokningsförsök (steg 15–16)

1. Flera kunder försöker boka överlappande perioder.
2. Systemet kontrollerar och sparar under databasskydd så att ett exemplar inte kan tilldelas två aktiva överlappande bokningar.
3. Om flera exemplar finns kan flera bokningar lyckas, men varje bekräftad bokning får ett eget bokningsbart exemplar.
4. Bokningar som inte kan tilldelas ett exemplar avvisas med begripligt besked.

### A8 – Sparandet misslyckas (steg 15–16)

1. Ett tekniskt fel uppstår när bokningen ska sparas.
2. Operationen avbryts utan att lämna en ofullständig bekräftad bokning eller reservation.
3. Systemet visar ett felmeddelande och kunden kan försöka igen.

### A9 – Personal bokar åt kund

1. Behörig personal identifierar kunden och väljer modell och period.
2. Systemet använder samma regler för datum, tillgänglighet, pris och dubbelbokning som vid kundens egen bokning.
3. Personalen bekräftar bokningen enligt överenskommet arbetssätt.
4. Systemet registrerar bokningen på kunden och tilldelar ett unikt bokningsnummer.

## 5. Resultat efter genomförandet

**Vid lyckad bokning:**

- Bokningen har sparats på rätt kund med unikt bokningsnummer och status `Confirmed`.
- Ett fysiskt exemplar har reserverats för bokningsperioden.
- Startdatum, slutdatum och överenskommet totalpris har sparats.
- Senare ändringar av modellens dagspris ändrar inte det sparade totalpriset.
- Ingen annan aktiv bokning kan reservera samma exemplar under en otillåten överlappande period.
- Kunden kan se sin bokning direkt i systemet.

**Vid misslyckad bokning:**

- Ingen ny bekräftad bokning eller reservation har skapats.
- Befintliga bokningar och reservationer är oförändrade.
- Kunden får ett begripligt besked om varför bokningen inte kunde genomföras.

## 6. Koppling till UML-diagram

**Aktivitetsdiagram:** `docs/uml/US-16/US-16-aktivitetsdiagram.puml`

Visar validering, tillgänglighetskontroll, bokningsbekräftelse, slutkontroll vid sparande och alternativa flöden.

**Sekvensdiagram:** `docs/uml/US-16/US-16-sekvensdiagram.puml`

Visar samspelet mellan `Kund`, `BookingView`, `BookingController`, `BookingService`, `BookingRepository` och `Databas`. Slutkontrollen vid sparande ska förhindra dubbelbokning.

**Klassdiagram:** `docs/uml/klassdiagram.puml`

Relevanta klasser eller motsvarande begrepp:

- `Booking` – kund, exemplar, period, status och överenskommet totalpris.
- `EquipmentModel` – modellinformation och dagspris.
- `EquipmentItem` – enskilt exemplar med inventarienummer.
- `Customer` – kunden som bokningen tillhör.
- `BookingService` – samordnar bokningsprocessen.
- `BookingRepository` – hämtar och sparar bokningar.

**Avstämning före inlämning:** Kontrollera vilken version av US-16-diagrammen gruppen väljer som slutlig. Diagrammen som tidigare visats saknar ett uttryckligt steg där totalpriset visas **före** kundens bekräftelse; komplettera dem så att de överensstämmer med detta användningsfall och K12. Klassnamn och metoder behöver också stämma med gruppens slutliga UML.

## 7. Designbeslut och antaganden

### 7.1 Verksamhetsregler och krav

- Inloggning krävs för kundbokning (K03).
- Kunden väljer modell och period; systemet kontrollerar tillgänglighet (K02).
- Bokningen gäller ett exemplar och ger bokningsnummer och direkt åtkomst till bokningsuppgifter (K04).
- Personalbokningar följer samma tillgänglighetsregler (K07).
- Otillgängliga exemplar får inte användas för nya bokningar (K09).
- Totalpris visas före bekräftelse och bevaras efter bokning (K12).
- Ingen dubbelbokning får uppstå, även vid samtidiga bokningsförsök.

### 7.2 Föreslagna tekniska designbeslut

**Bokningstjänst:** `BookingService` samordnar validering och skapande av bokningar.

**Tillgänglighetskontroll:** Systemet kontrollerar modellens fysiska exemplar, befintliga aktiva bokningar, otillgänglighet och regeln om fredag till måndag. Avbokade bokningar ska inte fortsätta blockera exemplaret.

**Automatisk tilldelning:** Kunden väljer modell; systemet väljer ett bokningsbart exemplar.

**Skydd mot dubbelbokning:** `BookingRepository.TrySaveBooking` föreslås genomföra en databasskyddad slutkontroll och lagring som en sammanhängande operation. En tidigare kontroll i gränssnittet är inte tillräcklig för att skydda mot samtidiga försök.

**Bokningsstatus:** En bekräftad bokning får status `Confirmed`; efter utlämning kan status bli `CheckedOut` och efter återlämning `Returned`.

**Pris:** Totalpriset beräknas utifrån dagspris och antal hyresdagar. Det överenskomna beloppet sparas som `Booking.AgreedTotalPrice` och påverkas inte av senare prisändringar.

**Felhantering:** Vid ogiltiga uppgifter, tillgänglighetskonflikt eller sparfel får kunden ett begripligt besked utan att en ofullständig bokning lämnas kvar.

### 7.3 Antaganden och öppna frågor

**Antaganden:**

- Varje fysiskt exemplar har ett unikt inventarienummer.
- Systemet tilldelar automatiskt ett bokningsbart exemplar.
- Bekräftade bokningar har status `Confirmed`.
- Bokningsnummer genereras automatiskt och är unika.
- Bokning och reservation sparas genom en databasskyddad operation.
- Överenskommet totalpris sparas som ett fast belopp på bokningen.

**Öppna frågor:**

- Hur ska regeln om fredag till måndag tillämpas på andra veckodagar och vid andra periodkombinationer?
- Ska bokningsbekräftelse skickas via e-post?
- Kan personal boka åt en kund som saknar konto?
- Hur ska personalens bokningar åt kunder bekräftas?

De tekniska lösningarna är designförslag som ska granskas mot gruppens UML-diagram och begränsade praktiska verifiering. Uppgiften kräver inte ett färdigt bokningssystem.
