<div align="center">

<img src="docs/images/banner.png" alt="Silentia" width="880">

<br><br>

<img src="https://img.shields.io/badge/Unity_6-3B4636?style=for-the-badge&logo=unity&logoColor=F6F0E1" alt="Unity 6">&nbsp;
<img src="https://img.shields.io/badge/C%23-BE7C49?style=for-the-badge&logo=csharp&logoColor=F6F0E1" alt="C#">&nbsp;
<img src="https://img.shields.io/badge/Windows-86A9B0?style=for-the-badge&logo=windows&logoColor=F6F0E1" alt="Windows">&nbsp;
<img src="https://img.shields.io/github/v/release/daria-sav/Silentia?style=for-the-badge&label=Versioon&color=BC8E5C&labelColor=3B4636" alt="Versioon">&nbsp;
<img src="https://img.shields.io/badge/Litsents-CC_BY--NC--ND_4.0-7B8674?style=for-the-badge" alt="Litsents">

<br>

### 2D mõistatus-platvormer, kus varasemad tegevused naasevad kajadena.

*Sinu tegevused ei kao jäljetult — need pöörduvad tagasi poolläbipaistvate kajadena, kes tegutsevad templis sinu kõrval.*

<br>

[Mängust](#mängust)&nbsp;&nbsp;·&nbsp;&nbsp;[Omadused](#omadused)&nbsp;&nbsp;·&nbsp;&nbsp;[Tegelased](#tegelased)&nbsp;&nbsp;·&nbsp;&nbsp;[Tehniline ülevaade](#tehniline-ülevaade)&nbsp;&nbsp;·&nbsp;&nbsp;[Alustamine](#alustamine)&nbsp;&nbsp;·&nbsp;&nbsp;[Edasiarendus](#edasiarendus)

<sub><a href="README.md">English</a>&nbsp;·&nbsp;Eesti keeles</sub>

</div>

---

<div align="center">
<img src="docs/images/silentia-gameplay.png" alt="Silentia mänguprotsess" width="760">
</div>

## Mängust

Silentia on 2D mõistatus-platvormer, mille keskmes on kloonipõhine mehaanika. Mängija juhib munka läbi unustatud templi katsumuste, kus mõned väljakutset ei saa ületada üksinda. Pühamus saab mängija astuda ühte munga sisemistest jõududest ehk *tšakrasse* ning salvestada selle liikumise. Mängumaailma naastes ilmub salvestus poolläbipaistva *kajana*, mis kordab samu tegevusi sünkroonis. Iga mõistatus lahendatakse munka ja kuni kolme kaja tegevusi koordineerides.

Kajad ei ole pelgalt taustaanimatsioonid, vaid mängumaailma tegelikud osalised. Nad vajutavad nuppe, hoiavad platvorme, jõuavad kohtadesse, kuhu munk ise ei pääse ning püsivad omavahel täpselt sünkroonis.

> [!NOTE]
> Silentia kujunes kahe testimisvooru käigus viie mängija tagasiside põhjal. Testijad mõistsid kloonipõhist mehaanikat iseseisvalt ning pidasid mängu raskusastet õiglaseks.

## Omadused

- **Kloonipõhine mehaanika**, mis salvestab mängija tegevused ja toob need tagasi poolläbipaistvate kajadena
- **Kolm mängitavat tšakrat**, igaühel oma liikumisvõime ja roll mõistatuste lahendamisel
- **Ajastusel põhinevad mõistatused**, kus tuleb koordineerida mitut kaja korraga
- **Sisendipõhine deterministlik taasesitussüsteem** koos kaadripõhise kõrvalekalde korrigeerimisega
- **Kuus käsitsi kujundatud taset**, mis tutvustavad mehaanikat samm-sammult
- **Algupärane pikselgraafikas maailm**, mis loob rahuliku ja meditatiivse templiatmosfääri
- Loodud **Unity 6** mängumootoris andmepõhise tegelaste arhitektuuriga

## Tegelased

Munk valdab ainult põhilist liikumist. Kajadena saab salvestada kolme tšakrat ning **igaüks neist saab korraga eksisteerida vaid ühe kajana**. Seetõttu sõltub iga mõistatus erinevate jõudude koostööst.

| | Tšakra | Element | Võime |
|:--:|:--|:--:|:--|
| <img src="docs/images/muladhara.png" width="66"> | **Muladhara** | Maa | **Topelthüpe.** Kindel ja tasakaalukas jõud, mis aitab jõuda kõrgemale. |
| <img src="docs/images/svadhisthana.png" width="66"> | **Svadhisthana** | Vesi | **Väike keha.** Kerge ja voolav jõud, mis mahub ka kõige kitsamatesse pragudesse. |
| <img src="docs/images/manipura.png" width="66"> | **Manipura** | Tuli | **Sööst.** Kiire ja terav jõud, mis paiskub üle suuremate vahemaade. |

## Tehniline ülevaade

Silentia kasutab sisendipõhist taasesitussüsteemi. See salvestab mängija sisendid, mitte tegelase asukoha, ning esitab need hiljem uuesti sama liikumis- ja füüsikasüsteemi kaudu, mis juhib ka munka. Tänu sellele on iga kaja mängumaailma päris osaline. Kui platvorm oli salvestamise ajal olemas, kuid taasesituse ajal puudub, kukub kaja alla.

Et taasesitus püsiks usaldusväärne, on simulatsioon üles ehitatud deterministlikult. Kogu liikumine ja füüsika toimuvad fikseeritud ajasammuga ning kõrvalekalde korrigeerimise süsteem joondab iga kaja igal kaadril tema salvestatud olekuga. Nii parandatakse väikeseid ujukomaarvutuste vigu, mis võivad pikemate salvestuste jooksul koguneda.

<details>
<summary><b>Lähem pilk taasesitussüsteemile</b></summary>

<br>

Tegevuste taasesitamiseks on kaks peamist lähenemist. **Olekupõhine** süsteem salvestab igal sammul tegelase täieliku oleku, sealhulgas asukoha, kiiruse ja sisemised väärtused. See lähenemine on töökindel, kuid mahukas, ning kloon kordab kindlat trajektoori pigem nagu videolõik. **Sisendipõhine** süsteem salvestab ainult mängija sisendid ja taastab ülejäänu simulatsiooni kaudu. Silentia kasutab sisendipõhist lähenemist, sest salvestatud sisendid saab suunata tagasi läbi sama süsteemi, mis juhib elavat tegelast. Nii osaleb kaja füüsikamaailmas, mitte ei korda lihtsalt külmutatud trajektoori.

**Determinism.** Sisendite taasesitus toimib ainult siis, kui samad sisendid annavad alati sama tulemuse. Seetõttu toimub kogu liikumine ja füüsika `FixedUpdate` meetodis fikseeritud ajasammuga, sõltumata kaadrisagedusest. Lisaks on komponentide täitmisjärjekord selgelt määratud.

**Salvestamine.** Iga `FixedUpdate` tsükli ajal kirjutab salvesti kaadrisse mängija praeguse sisendi. Samal ajal jäädvustatakse pärast füüsikasammu tegelase asukoha ja kiiruse hetktõmmised, mis salvestatakse perioodiliste võtmekaadritena.

**Taasesitus ja kõrvalekalde korrigeerimine.** Taasesituse ajal suunatakse salvestatud sisendid tagasi läbi sama liikumissüsteemi, mida kasutab elav munk. Pikkade salvestuste jooksul võivad siiski koguneda väikesed ujukomaarvutuste vead. Selle vähendamiseks võrdleb korrektor iga kaja pärast füüsikasammu tema salvestatud võtmekaadritega. Väikesed kõrvalekalded juhitakse sujuvalt tagasi salvestatud rajale, kuid suuri lahknevusi automaatselt ei parandata, sest need viitavad tõenäoliselt sellele, et mängumaailma olukord on muutunud.

**Tegelaste arhitektuur.** Tegelased on üles ehitatud andmepõhiselt. Ühine liikumissüsteem loeb iga tegelase parameetrid `ScriptableObject` failidest ning iga tegelane lubab ainult talle määratud võimeolekuid. Nii saavad munk ja kolm tšakrat töötada sama koodibaasi peal, säilitades samal ajal erineva juhtimistunde.

</details>

## Alustamine

### Allalaadimine Windowsile

Mängitav versioon on saadaval jaotises [**Releases**](https://github.com/daria-sav/Silentia/releases).

1. Ava uusim väljalase.
2. Laadi alla `Silentia_v1.0.0_Windows.zip`.
3. Paki fail lahti ja käivita `Silentia.exe`.

### Lähtekoodist ehitamine

Silentia on Unity projekt.

1. Klooni see repositoorium.
2. Ava projekt **Unity 6** või uuema versiooniga.
3. Ava algstseen ja vajuta **Play**.

### Juhtnupud

| Tegevus | Klahv |
|:--|:--|
| Liikumine | Nooleklahvid&nbsp;/&nbsp;WASD |
| Hüpe | Üles&nbsp;/&nbsp;W&nbsp;/&nbsp;Tühik |
| Sööst *(Manipura)* | X |
| Suhtlemine ja pühamusse sisenemine | E |
| Salvestuse lõpetamine | Q |
| Kõigi salvestatud kajade käivitamine | C |
| Pühamust lahkumine | Esc |
| Valitud pesa tühjendamine | Delete |

## Edasiarendus

Silentia on valminud bakalaureusetööna, kuid templis on veel arenguruumi.

**Testijate tagasiside põhjal**

- [ ] **Heli ja muusika.** Praegune versioon on helitu. Taustamuusika ja heliefektid olid testijate seas kõige sagedamini soovitud täiendused.
- [ ] **Elavam narratiiv.** Lugu võiks olla tihedamalt tasemetesse põimitud ning sisaldada tegelasi, kes juhivad munka templis sügavamale.
- [ ] **Rohkem avastamist.** Mängu võiks laiendada lisatasemete ja mitmekesisemate templikeskkondadega.

**Edasised ideed**

- [ ] **Avatav tegelaskond.** Iga läbitud templiosa võiks avada uue tšakra.
- [ ] **Vaenlased ja võitlus.** Kerge võitluskiht võiks muuta väljakutseid mitmekesisemaks.
- [ ] **Peidetud kogumisesemed.** Valikulised auhinnad võiksid paikneda tasemete kõige keerulisemates nurkades.
- [ ] **Läbimängu statistika.** Mäng võiks jälgida surmade arvu, läbimisaegu ja tasemepõhist edenemist.

## Tehnoloogiad

- **Unity 6** - mängumootor ja arenduskeskkond
- **C#** - mänguloogika programmeerimine
- **Aseprite** - pikselgraafika ja animatsioon
- **Figma** - kasutajaliidese kujundus

## Autorid

- **Daria Savtšenko** - mängudisain, programmeerimine ja tasemete kujundus
- **Sofia Savtšenko** - kunst ja visuaalne kujundus
- **Mark Muhhin** - lõputöö juhendaja

Liikumissüsteemi alus on kohandatud projektist [DawnosaurDev / platformer-movement](https://github.com/DawnosaurDev/platformer-movement), mida kasutatakse MIT-litsentsi alusel.

Silentia valmis bakalaureusetööna Tartu Ülikooli arvutiteaduse instituudis 2026. aastal.

## Litsents

Silentia on avaldatud litsentsi **Creative Commons Autorile viitamine-Mitteäriline eesmärk-Tuletatud teoste keeld 4.0 Rahvusvaheline** (CC BY-NC-ND 4.0) alusel. Projekti võib jagada autorile viidates, kuid seda ei tohi kasutada ärilisel eesmärgil ega levitada muudetud kujul.

Kolmanda osapoole materjalid jäävad oma litsentside alla. Täpsem teave on failides [LICENSE.md](LICENSE.md) ja [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).

---

<div align="center">
<sub><b>Silentia</b> · Bakalaureusetöö Tartu Ülikoolis · Arvutiteaduse instituut · 2026</sub>
</div>
