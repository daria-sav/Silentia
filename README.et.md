<div align="center">

<img src="docs/images/banner.png" alt="Silentia" width="880">

<br><br>

<img src="https://img.shields.io/badge/Unity_6-3B4636?style=for-the-badge&logo=unity&logoColor=F6F0E1" alt="Unity 6">&nbsp;
<img src="https://img.shields.io/badge/C%23-BE7C49?style=for-the-badge&logo=csharp&logoColor=F6F0E1" alt="C#">&nbsp;
<img src="https://img.shields.io/badge/Windows-86A9B0?style=for-the-badge&logo=windows&logoColor=F6F0E1" alt="Windows">&nbsp;
<img src="https://img.shields.io/github/v/release/daria-sav/Silentia?style=for-the-badge&label=Versioon&color=BC8E5C&labelColor=3B4636" alt="Versioon">&nbsp;
<img src="https://img.shields.io/badge/Litsents-CC_BY--NC--ND_4.0-7B8674?style=for-the-badge" alt="Litsents">

<br>

### 2D mõistatus-platvormer, kus su varasemad tegevused naasevad kajadena.

*Su tegevused ei haihtu kunagi — need naasevad poolläbipaistvate varasemate minadena, kes lahendavad templit su kõrval.*

<br>

[Mängust](#mängust)&nbsp;&nbsp;·&nbsp;&nbsp;[Omadused](#omadused)&nbsp;&nbsp;·&nbsp;&nbsp;[Tegelased](#tegelased)&nbsp;&nbsp;·&nbsp;&nbsp;[Tehniline ülevaade](#tehniline-ülevaade)&nbsp;&nbsp;·&nbsp;&nbsp;[Alustamine](#alustamine)&nbsp;&nbsp;·&nbsp;&nbsp;[Edasiarendus](#edasiarendus)

<sub><a href="README.md">English</a>&nbsp;·&nbsp;Eesti keeles</sub>

</div>

---

<div align="center">
<img src="docs/images/silentia-gameplay.png" alt="Silentia mänguprotsess" width="760">
</div>

## Mängust

Silentia on 2D mõistatus-platvormer, mille keskmes on kloonipõhine mehaanika. Vaikne munk läbib unustatud templi katsumusi, kus ühtki väljakutset ei saa ületada üksinda. Pühamus astub mängija ühte munga sisemistest jõududest, *tšakrasse*, ning salvestab selle liikumise. Maailma naastes ilmub see salvestus poolläbipaistva *kajana*, mis kordab samu tegevusi sünkroonis. Iga mõistatus lahendatakse, koordineerides munka kuni kolme kajaga korraga.

Kajad on tõelised osalised, mitte taustaanimatsioon. Nad vajutavad nuppe, hoiavad platvorme, jõuavad kohtadesse, kuhu munk ei pääse, ning püsivad omavahel täpselt sünkroonis.

> [!NOTE]
> Silentia kujunes kahe testimisvooru käigus viie mängijaga, kes mõistsid kloonimehaanikat iseseisvalt ja pidasid raskusastet õiglaseks.

## Omadused

- **Kloonipõhine mehaanika**, mis salvestab su tegevused ja esitab need taas poolläbipaistvate kajadena
- **Kolm mängitavat tšakrat**, igaühel oma liikumisvõime
- **Mõistatused, mis põhinevad ajastusel** ja mitme kaja üheaegsel koordineerimisel
- **Sisendipõhine deterministlik taasesitussüsteem** koos kaadrihaaval toimuva kõrvalekalde korrigeerimisega
- **Kuus käsitsi kujundatud taset**, mis õpetavad mehaanikat samm-sammult
- **Algupärane pikselkunstis maailm** rahulikus, meditatiivses templis
- Loodud **Unity 6**-s andmepõhise tegelaste arhitektuuriga

## Tegelased

Munk kasutab ainult põhilist liikumist. Kajadena saab salvestada kolme tšakrat ja **igaüks neist eksisteerib vaid ühes eksemplaris**, mistõttu iga mõistatus sõltub erinevate jõudude koostööst.

| | Tšakra | Element | Võime |
|:--:|:--|:--:|:--|
| <img src="docs/images/muladhara.png" width="66"> | **Muladhara** | Maa | **Topelthüpe.** Maine ja kindel, ulatub kõrgele üles. |
| <img src="docs/images/svadhisthana.png" width="66"> | **Svadhisthana** | Vesi | **Väike keha.** Kerge ja voolav, mahub kõige kitsamatesse pragudesse. |
| <img src="docs/images/manipura.png" width="66"> | **Manipura** | Tuli | **Sööst.** Kiire ja terav, paiskub üle avara vahemaa. |

## Tehniline ülevaade

Silentia kasutab sisendipõhist taasesitussüsteemi. See salvestab mängija sisendid, mitte tema asukoha, ning esitab need taas sama füüsikamootori kaudu, mis juhib munka. See muudab iga kaja maailma tõeliseks osaliseks, mitte eelsalvestatud klipiks. Kui platvorm oli salvestamise ajal olemas, kuid puudub taasesitusel, siis kaja kukub.

Et püsida ajas usaldusväärsena, on simulatsioon deterministlik. Kogu liikumine ja füüsika töötab fikseeritud ajasammul ning kõrvalekalde korrigeerimise süsteem joondab iga kaja igal kaadril uuesti tema salvestatud olekuga, parandades ujukomavea, mis pikkade salvestuste jooksul koguneb.

<details>
<summary><b>Lähem pilk taasesitussüsteemile</b></summary>

<br>

Tegevuste taasesitamise süsteemi jaoks on kaks lähenemist. **Olekupõhine** süsteem salvestab igal sammul tegelase täieliku oleku, sealhulgas asukoha, kiiruse ja sisemised väärtused. See on töökindel, kuid mahukas, ning kloon kordab vaid kindlat trajektoori, nagu videolõik. **Sisendipõhine** süsteem salvestab ainult mängija sisendid ja taastab kõik muu simulatsiooni teel. Silentia kasutab sisendipõhist lähenemist, sest salvestatud sisendid saab suunata tagasi läbi sama mootori, mis liigutab elavat tegelast, nii et kaja osaleb füüsikamaailmas, mitte ei korda külmunud trajektoori.

**Determinism.** Sisendite taasesitus toimib ainult siis, kui samad sisendid annavad alati sama tulemuse. Seetõttu toimub kogu liikumine ja füüsika `FixedUpdate` sees fikseeritud ajasammuga, sõltumatult kaadrisagedusest, ning komponentide täitmisjärjekord on selgelt fikseeritud.

**Salvestamine.** Igal `FixedUpdate`-l kirjutab salvesti praeguse sisendi kaadrisse. Paralleelselt jäädvustatakse asukoha ja kiiruse hetktõmmised *pärast* füüsikasammu ning salvestatakse perioodiliste võtmekaadritena.

**Taasesitus ja kõrvalekalde korrigeerimine.** Taasesituse ajal suunatakse salvestatud sisendid tagasi läbi sama liikumissüsteemi, mida kasutab elav munk. Ujukomaviga koguneb pikkade salvestuste jooksul siiski, mistõttu korrektor võrdleb iga kaja pärast iga füüsikasammu tema võtmekaadritega. Väikesed kõrvalekalded interpoleeritakse sujuvalt tagasi salvestatud rajale, suured lahknevused jäetakse aga puutumata, sest need tähendavad, et muutunud on tase ise, mitte arvutus.

**Tegelaste arhitektuur.** Tegelased on andmepõhised. Ühine liikumismootor loeb iga tegelase parameetrid `ScriptableObject` failidest ning iga tegelane lubab ainult oma võimete olekud. Nii töötavad munk ja kõik kolm tšakrat ühel koodibaasil, säilitades samas erineva juhtimistunde.

</details>

## Alustamine

### Allalaadimine (Windows)

Mängitav versioon on saadaval jaotises [**Releases**](https://github.com/daria-sav/Silentia/releases).

1. Ava uusim väljalase.
2. Laadi alla `Silentia_v1.0.0_Windows.zip`.
3. Paki fail lahti ja käivita `Silentia.exe`.

### Lähtekoodist ehitamine

Silentia on Unity projekt.

1. Klooni see repositoorium.
2. Ava see **Unity 6**-ga või uuemaga.
3. Ava algstseen ja vajuta **Play**.

### Juhtnupud

| Tegevus | Klahv |
|:--|:--|
| Liikumine | Nooleklahvid&nbsp;/&nbsp;WASD |
| Hüpe | Üles&nbsp;/&nbsp;W&nbsp;/&nbsp;Tühik |
| Sööst *(Manipura)* | X |
| Suhtlemine · pühamusse sisenemine | E |
| Salvestuse lõpetamine | Q |
| Kõigi salvestatud kajade käivitamine | C |
| Pühamust lahkumine | Esc |
| Valitud pesa tühjendamine | Delete |

## Edasiarendus

Silentia on lõputööna valmis, kuid templil on veel arenguruumi.

**Testijate tagasiside põhjal**

- [ ] **Heli ja muusika.** Praegune versioon on helitu; taustamuusika ja heliefektid on enim soovitud täiendus.
- [ ] **Elav narratiiv.** Loo põimimine tasemetesse endisse, koos tegelastega, kes juhivad munka templis sügavamale.
- [ ] **Rohkem avastada.** Lisatasemed ja mitmekesisemad templikeskkonnad.

**Edasised ideed**

- [ ] **Avatav tegelaskond.** Iga läbitud templiga teenitakse uus tšakra.
- [ ] **Vaenlased ja võitlus.** Kerge võitluskiht väljakutse mitmekesistamiseks.
- [ ] **Peidetud kogumisesemed.** Valikulised auhinnad taseme kõige keerulisemates nurkades.
- [ ] **Läbimängu statistika.** Surmad, läbimisajad ja tasemepõhine edenemine.

## Tehnoloogiad

- **Unity 6** · mängumootor ja tööriistad
- **C#** · mänguloogika programmeerimine
- **Aseprite** · pikselgraafika ja animatsioon
- **Figma** · kasutajaliidese kujundus

## Autorid

- **Daria Savtšenko** · mängudisain, programmeerimine ja tasemete kujundus
- **Sofia Savtšenko** · kunst ja visuaalne kujundus
- **Mark Muhhin** · lõputöö juhendaja

Liikumissüsteemi alus on kohandatud projektist [DawnosaurDev / platformer-movement](https://github.com/DawnosaurDev/platformer-movement), mida kasutatakse MIT-litsentsi alusel.

Silentia valmis bakalaureusetööna Tartu Ülikooli arvutiteaduse instituudis 2026. aastal.

## Litsents

Silentia on avaldatud litsentsi **Creative Commons Autorile viitamine-Mitteäriline eesmärk-Tuletatud teoste keeld 4.0 Rahvusvaheline** (CC BY-NC-ND 4.0) alusel. Projekti võib jagada autorile viidates, kuid seda ei tohi kasutada ärilisel eesmärgil ega levitada muudetud kujul.

Kolmanda osapoole materjalid jäävad oma litsentside alla. Täpsem teave failides [LICENSE.md](LICENSE.md) ja [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).

---

<div align="center">
<sub><b>Silentia</b> · Bakalaureusetöö Tartu Ülikoolis · Arvutiteaduse instituut · 2026</sub>
</div>
