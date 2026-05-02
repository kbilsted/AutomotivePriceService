# AutomotivePriceService

# Opgave 1


Opgaven viser hvorledes man kan lave et endpoint der tager imod bulk af data. 




## Antagelser til funktionalitet

- Der er ikke performancekrav til selve importen - dvs vi skal ikke tænkte i parallelisering eller bulk indsættelser
- En delvist-korrupt fil skal så vidt muligt importeres fremfor hele leverancen skal fejle
- Vi ønsker at kunne spore en pris på en bil tilbage til den leverance prisen kom fra
- Korrupt data må ignoreres
- Fil med tom liste af priser er valid


### Post endpoint

Import Endpointet designes som idempotent: Sendes samme payload flere gange returneres blot status 200 "OK" med passende fejltekst (der kan ignoreres). 

Fejl i header returner 400 bad request. Fejl i record payload behandles på record niveau, dvs fejl returnerer stadig status 200 "OK", med en tæller for antal fejl. Alternativ kunne man fejle hele leverancen ved en record fejl ved at returnere 400 "Bad request".


### Get endpoint

Hentning af pris endpoint returneres status 200 "OK" med et beløb, valuta, og en sporing på hvorfra prisen stammer. Hvis entiteten ikke findes returneres status 404 "Not found" idet forespørgslen er på en ID. 

Havde endpoint været et søge-endpoint, ville der ved søgning på en model der ikke findes eller der ikke findes en pris, returneres status 200 "OK", og en tom liste fremfor 404 "Not found".



## One thing you would do differently if this were going into production at xx 

Bedre logning med struktureret data og bedre separation af logik og controllere. Pt. er koden præget af tidspres.



##  How you would monitor this service, so we would know within minutes if something went wrong overnight

Anvende et logsystem såsom splunk eller tilsvarende log system i Azure (Azure monitor logs). Der kan man let opsætte automatiserede alarmer, der kan kommunikere med email, web-hook osv. Integrationen vil være log-filer fra services ingest'es ind i logsystemet. 


## One thing you deliberately skipped because of the time limit, and why that was the right thing to skip

- Der mangler sikkerhed på endpoint, f.eks. JWT og IP-range beskyttelse


Her er de forbedringer jeg kunne ønske mig


- Få afklaret flere krav før implementation start
- Mulighed for at kunne tilrette korrupte data fremfor at ignorere dem
- Exception håndtering mangler pt, og skal sikre at service håndterer fejl ordenligt og ikke eksponerer for meget
- Bedre dokumentation i swashbuckle af endpoints og statuskoder. pt er der intet 
- Indføre konfiguration for valideringer - valideringer i kode bliver hurtigt uoverskueligt, derfor vil validering som konfiguration eller i det mindste et valideringsframework være godt
- Indføre flere automatiserede test - lige nu testes blot end-to-end
- Indføre ordenlig logning på request-response niveau fremfor manuel logning der gør koden svær at læse






# Opgave 2

![opgave 2](opgave2.svg "opgave2")

Dette beskriver kort et design til en pris/aktie-import funktionalitet. 


### Fil ind 

I dag uploades filer via FTP til en intern server. I det nye design skal kunden anvende samme url og transportmetode, men uploade direkte til Azure til en Azure file blob storage. 

Vi opbevarer alle filer råt for at dokumentere præcis hvad vi har modtaget og hvornår. Det sikrer også vores import kan være idempotent - altså kunne håndtere at modtage den samme fil flere gange. 


En Azure function aktiveres ved upload. Denne funktion sender et event **FileReadyForImport** på service bussen og intet andet. Dette sikrer, at opdagelsen af en fil separeres fra processeringen af en fil. Det gør det også lettere manuelt at retry en filimport, da vi blot skal sende eventet manuelt. Eventet vil ikke holde data filen, men kun en blob ID.

En ny azure function lytter på **FileReadyForImport** og gør følgende

* Sikrer at filen/leverancen ikke før er blevet importeret ved at kigge i en SQL database for hashværdien for filen.
* Herefter danner den relevante leverancefelter, felt et leveranceid, og et correlationid og måske et "filnavn" der skal anvendes i den senere kontrol. Disse gemmes i en tabel for leverancer med tidspunkt på start.
* Logger aktiviteten er startet
* Filen indlæses og rækker valideres og flyttes over i en eller flere tabeller i sql basen. F.eks. kan der være tabeller for gode data, og en for invalide  data.
* Der logges pr record til en senere alarmering og til at udviklere let kan gruppere fejltyper for et nuanceret fejlbillede
* De invalide records kan herefter tilrettes via en gui eller endnu en fil upload.
* Ved invalid data logges data til den senere alarmering.
* Der gemmes i leverance tabel tidsstempel for data  import, og dermed hvis filen uploades igen, kan den ignoreres.
* Logninger ved start og slut af funktionen dokumenterer importens afvikling, og anvendes til løbende at monitorere om processering er for tidskrævende.



#### Overvejelser
* Vi skal passe på at importen ikke tager for lang tid, da funktionen vil blive dræbt inden for 10 min. og import vil genforsøges. Dvs vi kan være sårbare overfor store filer. Vi skal også sikre os låsetiden for selve eventet så andre functions ikke overtager eventet.
* Opsplitning via servicebus muliggør skallering. Vi kan have flere azure functions der i parallel processerer hvis flere filer uploades samtidig
* Hvis en kodefejl får importen til at crashe forbliver event på service bussen og en ny function instans vil opsamle den. Dette gør løsningen robust, men det kan måske blive dyrt at et import job står og fejler, crasher og prøver igen hele natten. Vi kan imødekomme noget af dette ved at sikre os at vi har en max retries for en given leverance - dette skal håndteres med en kolonne der gemmes i sql basen.
* Alternativ til at håndtere hele filen via et event, kan vi opslitte filen i records og sende et event pr. record. Det kan gøre det sværere at vide hvornår en fil er importeret, og det kan gøre det svært at holde performance oppe da det vil føre til mange connections og små indsættelser i mange transaktioner. 

 


### Opdage fejl

Jeg har erfaringer med at opsætte alarmer i logsystemet, på baggrund af manglende logninger. Om det kan anvendes her, er under antagelse af reglerne for upload er simple. Dvs et "fast" antal kilder der uploader med fast frekvens. Ellers kan vi ikke let udtrykke alarmerne i logsystemet. I Alka forsikring, har vi haft meget forskellige regler for de enkelte kilder, og måtte derfor implementere kompliceret logik for at opdage manglende leverancer.

Så alternativt kan vi implementere en scheduled azure function der aktiveres om natten og kigger i databasen efter manglende leverancefil og/eller manglende tidsstempel for færdigimport udfra viden om hvilke leverancder er forventes på hvilke dage og hvad deres leverancefiler i så fald vil hedde. Ved manglende filer logges til log systemet der står for alarmeringen.

Dette frakobler dermed opdagelse fra alarmering og sikrer, at den ene del kan ændres uafhængigt af den anden. F.eks. hvis der ønskes at informere flere parter, skal dette ske udelukkende i log systemet.



### Genbehandling af filer
Genupload af filer vil blive ignoreret (som i opgave 1) hvis indhold er helt identisk og filen færdigimporteret. Er der foretaget korrektioner i filen vil disse blive behandlet som nye værdier og indsat (hvis de er lovlige). Eksisterende værdier vi blive genindsat. 

Feltet "validFrom" sikrer at vi kan genindlæse gamle filer, idet priser vil gælde for bagud i tid og ikke for i dag. Hvis "validfrom" er feks for dags dato eller i fremtiden vil importen dog overskrive potentielt nyere indlæsninger.

Vi skal også sikre os, at ved upload af korrektioner skal der matches med tabellen med korrupt data såles korrigerede records slettes fra tabellen.



## Which Azure services you would use and why those, specifically — not just "because Azure has it"

Jeg har ikke kendskab til Azure services. Jeg har derfor trukket på mine eksisternde erfaringer og fundet de tilsvarende komponenter i Azure. Dvs. 

* Vi har et behov for at modtage på FTP (azure blob storage), 
* Vi har et behov for events (azure service bus) 
* Vi har en database til læsninger af forretningsenheder (Azure Sql).
* Vi har et logsystem til opsætning af alarmer og notifikationer (Azure monitor logs)

Når der flyttes fra interne servere til Azure skal man være opmærksom på vender lock-in og især ved store datamængder, kan flyt til anden leverandør være svær.  Flyttet er derfor en forretningsbeslutning i hvilken grad man ønsker cloud baseret infrastruktur eller om man ønsker at holde det tættere på forretningen.



## What happens when the partner's file is late, missing, or malformed — how does the system behave, and who notices what?

Tanken er, at der i logsystemet kan konfigureres en alarm på at mangler der logninger, alarmeres. Alarmeringen kan tage form af emails, webhooks eller andet. Jeg vil anbefale at der informeres internt i organisationen samt der sendes email til data-partneren. 




## How we would know within minutes — not the next morning — that something has gone wrong

Jeg vil anvende et logsystem opsat til at alarmere på visse typer af logninger. Dvs. logninger om at der er gået noget galt. Tilsvarende, kan der alarmeres hvis der mangler logninger om at det er gået godt i et bestemt tidsrum.


## A rough cost estimate per month. A ballpark is genuinely fine. "Probably a few hundred kroner, dominated by X" is a perfectly good answer.

Jeg kender ikke til priser på azure

