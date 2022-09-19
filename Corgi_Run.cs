using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

class Corgi : PhysicsObject
{
    private int MaxHP;
    private int MaxMP;
    private int MaxST;
    private int HP;
    private int MP;
    private int ST;
    private int Atk = 10;
    private int Def = 10;
    private int lvl = 1;
    private int Maxlvl = 100;
    private int Exp = 0;
    private string nimi;
    private double nopeus = 1.0;
    private bool init;
    //private Corgery[]; 


    public Corgi(double lev, double kor) : base(lev, kor)
    {
        Restitution = 0.30;
        KineticFriction = 0.95;
        Mass = 1.0;
        CanRotate = false;
        Shape = Shape.Circle;
        Color = Color.Orange;
    }

    public Corgi Ella(double koko, int stat)
    {
        Corgi corgi = new Corgi(koko, koko);
        nimi = "Ella";
        if(init == false)
        {
            MaxHP = (int)(stat * 1.5);
            MaxMP = stat;
            MaxST = stat;
            HP = MaxHP;
            MP = MaxMP;
            ST = MaxST;
            Def = 15;
        }

        init = true;
        return corgi;
    }

    public Corgi Ronja(double koko, int stat)
    {
        Corgi corgi = new Corgi(koko, koko);
        nimi = "Ronja";
        if (init == false)
        {
            MaxHP = (int)(stat * 1.5);
            MaxMP = stat;
            MaxST = stat;
            HP = MaxHP;
            MP = MaxMP;
            ST = MaxST;
            nopeus = 1.2;
        }

        init = true;
        return corgi;
    }
}

/// @author Aleksi Joutsen
/// @version 1.0
/// <summary>
/// Peli
/// </summary>
public class Corgi_Run : PhysicsGame
    {
        private PhysicsObject pelaaja; //Pelaajan fysiikkaolio
        private PhysicsObject isoVihu; //Kentän bossin fysiikkaolio

        private DoubleMeter pelaajaHP; //Pelaajan HP arvo, tämänhetkinen
        private DoubleMeter pelaajaMP; //Ylempi mutta MP
        private DoubleMeter pelaajaStamina; //Ylempi mutta stamina
        private DoubleMeter expa; //Ylempi mutta expa, vaaditaan leveleihin
        private DoubleMeter bossHP; //Bossin HP, tämänhetkinen
        private DoubleMeter edistyminen;//Max 100, -> pääsee kentässä eteenpäin
        private DoubleMeter aikaAjastin; //Paljon on aikaa päästä kenttä läpi

        private const int maxHP = 1500;    //Pelaajan HP-katto, tämän yli ei voi mennä
        private const int maxMP = 1000;   //ylempi mutta MP
        private const int maxStamina = 1500; //Ylempi mutta stamina
    private const int defaultStat = 100;
        private double leveli = 1; //Pelaajan leveli, tätä käytetään muun muassa stattien kehittämiseen ja damagen kasvattamiseen
        private const int levelMAX = 20; //Pelaajan max level
        private const int kenttiaPelissa = 4; //0 kenttä on ns. salainen kenttä
        private const int spellit = 2;
        private const double viive = 3.1;
        private int kentta = 0; //Kenttämuuttuja, jolla peli tietää mikä kenttä menossa

        private bool voiHypata = true; //Tämä tarkistaa onko pelaajalla oikeus hypätä uudestaan
        private bool voiVahingoittaa = false; //Tämä tarkistaa onko pelaaja tekemässä dashia eli oikeutettu tuhoamaan tai vahingoittamaan vihuja
        private bool damageSuoja = false; //Tämä tarkistaa onko pelaaja suojassa toistuvilta osumilta, jos päällä niin pelaaja ei ota vahinkoa
        private bool bossDamageSuoja = false; //Tarkistaa onko bossilla damagesuoja, katso ylempi
        private bool bossiRajoitin = false; //Tämä rajoittaa bossin toimintoja, ilman tätä Jypeli ei varmaan tarkista tarpeeksi nopeasti asioita ja bossit muuttuvat vaikeiksi
        private bool ammusRajoitin = false;
        private bool maali = false; //Tämä on tarkistus onko pelaaja oikeutettu menemään maaliin
        private bool tgm = false; //Tämä on ns. pelin debug homma, tekee kuolemattomaksi
        private bool[] kentatLapi = new bool[kenttiaPelissa]; //Tarkistaa onko pelaaja päässyt kenttiä läpi
        private double[] arvosanat = new double[kenttiaPelissa]; //Pelaajan saamat arvosanat kentissä, asteikko 0-5
        private readonly int[] bossTeksti = new int[kenttiaPelissa] { 430, 340, 330, 570 }; //Boss tekstikenttien koko per boss Pistä nämä boss-luontiin, koska turhia täällä
        private readonly string[] bossNimi = new string[kenttiaPelissa] { "Sir Hugelus McLennart, Kosmoksen syöjä", "Daesnoum, Kaaoksesta syntynyt", "Dangrian, Pimeyteen hukkunut", "Æn perijä Corbilius, Rautainen demonijumala" }; //Bossien nimet
        private bool suuntaOikea; //Tarkistus että meneekö pelaaja oikealle, tämä on dashin ja mahdollisten grafiikkojen takia
        private readonly string[] spellNimi = new string[spellit] { "Cure", "Ammus-Volley" };
        private bool[] spelliAvattu = new bool[spellit] { true, false };
        private int valinta = 0;
        private bool ronja = false;

        private Timer luodit; //Timeri bossin hyökkäyksiin, aina kun on täynnä niin lähtee luotiSade
        private Label info; //Tämä on tekstikenttä joka näkyy oikealla ylhäällä, tässä näkyy oleellista tietoa
        private Label spelli;
        private Timer aika; //Kuinka paljon aikaa kentässä on
        private int osumat; //Kuinka monta osumaa pelaaja on ottanut kentän aikana, tätä käytetään kentän suoritusarvioinnissa

        /// <summary>
        /// Pääohjelma
        /// </summary>
        public override void Begin()
        {
            AlustaMuuttujat();
            LuoAlkuValikko();
        }



        /// <summary>
        /// Luo kentän käyttämällä SetTileMethodia eli muuttaa pikselitaiteen olioiksi. Nollaa "maalin" tilanteen eli bossiin ei voi mennä ennen kuin 10 nappulaa kerätty.
        /// </summary>
        private void LuoKentta()
        {
            ClearAll(); //Poistetaan kaikki
            maali = false; //Tämä poistaa maaliin menemisen mahdollisuuden
            tgm = false; //Tämä poistaa kuolemattomuuden jos se nyt jää lopulliseen peliin
            ColorTileMap ruudut = ColorTileMap.FromLevelAsset("Kentta" + kentta); //tähän kenttaTiedostonNimi

            ruudut.SetTileMethod(Color.BrightGreen, LuoPelaaja);
            ruudut.SetTileMethod(Color.Black, LuoTaso);
            ruudut.SetTileMethod(Color.Red, LuoVihu);
            ruudut.SetTileMethod(Color.DarkRed, LuoIsoVihu);
            ruudut.SetTileMethod(Color.Gray, LuoTykki);
            ruudut.SetTileMethod(Color.Gold, LuoNappula);
            ruudut.SetTileMethod(Color.BloodRed, LuoPotion);
            ruudut.SetTileMethod(Color.Cyan, LuoMaali);
            ruudut.SetTileMethod(Color.DarkBlue, LuoBossEntry);
            ruudut.SetTileMethod(Color.Orange, LuoPickUp);
            ruudut.SetTileMethod(Color.Olive, LuoSalaisuus);
            if (kentta == 0)
            {
                MediaPlayer.Play("Piano");
            }
            else
            {
                MediaPlayer.Play("biisi1"); //Tiedän huono soundi
            }

            ruudut.Execute(100, 100); //Tämä luo kentän
        }

        /// <summary>
        /// Luo peliin tason, käyttämällä Kenttä+x tiedostoa
        /// </summary>
        /// <param name="paikka">Ottaa sijainnin mihin taso luodaan</param>
        /// <param name="x">Ei käytetä</param>
        /// <param name="y">Ei käytetä</param>
        private void LuoTaso(Vector paikka, double x, double y)
        {
            PhysicsObject taso = PhysicsObject.CreateStaticObject(100, 100); //Määritykset
            taso.Position = paikka;
            taso.Restitution = 0.20;
            taso.KineticFriction = 0.30;
            taso.IgnoresGravity = true;
            taso.CollisionIgnoreGroup = 2;
            Add(taso);
        }

        /// <summary>
        /// Luo maalin, johon osuessa pelaaja pääsee kentän läpi ja voi siirtyä seuraavaan kenttään.
        /// </summary>
        /// <param name="paikka">Sijainti, johon maali tulee</param>
        /// <param name="x">Ei käytössä</param>
        /// <param name="y">Ei käytössä</param>
        private void LuoMaali(Vector paikka, double x, double y)
        {
            LuoStaattinenObjekti(paikka, 50, Shape.Rectangle, Color.Transparent, "maali");
        }

        /// <summary>
        /// Luo kohdan, johon "Boss entry" spawnaa. Tämän funktio on estää pelaajaa menemästä bossiin ennen kuin "edistyminen" on tehty.
        /// Boss entryn ylittäminen luo mappiin bossiin liittyvät mekanismit ks. BossinAlku, LuoPalkkiBossHP, LuoBossTeksti
        /// </summary>
        /// <param name="paikka"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void LuoBossEntry(Vector paikka, double x, double y)
        {
            PhysicsObject entry = new PhysicsObject(50, 50); //Bossin aktivointipalikan määreet
            entry.Position = paikka;
            entry.Shape = Shape.Rectangle;
            entry.Color = Color.Transparent;
            entry.Tag = "BossEntry";
            entry.IgnoresGravity = true;
            entry.IgnoresPhysicsLogics = true;
            entry.CollisionIgnoreGroup = 1;
            Add(entry);
        }

        /// <summary>
        /// Luo aikalaskurin peliin, joka näkyy vasemmassa ylälaidassa. Aikalaskurin saavuttaessa 0, pelaaja häviää pelin, mutta ei kuole.
        /// LaskeAlaspäin -aliohjelma laittaa laskurin liikkumaan alaspäin.
        /// </summary>
        /// <param name="x">Aika, josta laskuri aloittaa tikittämisen</param>
        private void LuoAikaLaskuri(int x)
        {
            aikaAjastin = new DoubleMeter(x); //Tämä ajastin siis laskee kentän ajan

            aika = new Timer();
            aika.Interval = 0.1;
            aika.Timeout += LaskeAlaspain; //Timeout vähentää aikaa 0,1s koska Jypeli laskee vaan ylöspäin normaalisti
            aika.Start();

            Label aikaNaytto = new Label(); //Näytön määreet
            aikaNaytto.X = Screen.Left + 145;
            aikaNaytto.Y = Screen.Top - 95;
            aikaNaytto.TextColor = Color.Black;
            aikaNaytto.DecimalPlaces = 1;
            aikaNaytto.BindTo(aikaAjastin);
            Add(aikaNaytto);
        }

        /// <summary>
        /// Laittaa laskurin laskemaan alaspäin. Kun laskuri on 0, peli on hävitty, mutta pelaaja ei kuole.
        /// </summary>
        private void LaskeAlaspain()
        {
            aikaAjastin.Value -= 0.1; //Laskee laskuria alaspäin, koska jypeli osaa normaalisti laskea vain ylöspäin

            if (aikaAjastin.Value <= 0)
            {
                TilanneKatsaus(400, "Vainu kadotettu..."); //Peli hävitty mutta pelaaja ei kuollut
                aikaAjastin.Stop();
                LuoAlkuValikko(); //Palataan alkuun, mutta ei alusteta muuttujia, koska pelaaja ei kuollut
            }
        }

        /// <summary>
        /// Aliohjelma, joka luo pelaajahahmon.
        /// </summary>
        /// <param name="paikka">Paikka mihin pelaaja syntyy</param>
        /// <param name="x">Ei käytössä</param>
        /// <param name="y">Ei käytössä</param>
        private void LuoPelaaja(Vector paikka, double x, double y)
        {
            pelaaja = new PhysicsObject(90.0, 40.0); //Pelaajan määreet
            pelaaja.Position = paikka;
            pelaaja.Shape = Shape.Circle;
            pelaaja.Color = Color.Orange;
            pelaaja.Restitution = 0.30;
            pelaaja.KineticFriction = 0.95;
            pelaaja.Mass = 1.0;
            pelaaja.CanRotate = false;
            AddCollisionHandler(pelaaja, Tormays);  //Pelaaja voi hypätä uudestaan kun osuu johonki. Mahdollistaa Wall-jumpin, mutta myös kehittyneitä temppuja kuten vihusta triplahyppäämisen
            AddCollisionHandler(pelaaja, "vihollinen", VihuOsuma); //Pelaaja osuu viholliseen
            AddCollisionHandler(pelaaja, "bossi", BossiOsuma); //Pelaaja osuu bossiin
            AddCollisionHandler(pelaaja, "nappula", PisteLisaa); //Pelaaja osuu edistys-nappuloihin (tähti)
            AddCollisionHandler(pelaaja, "potion", MPpalautus); //Pelaaja osuu "potioniin" (sydän)
            AddCollisionHandler(pelaaja, "maali", MaaliinMeno); //Pelaaja osuu maaliin, joka vie seuraavaan kenttään
            AddCollisionHandler(pelaaja, "BossEntry", BossinAlku); //Bossin aloittavaan juttuun osuminen
            AddCollisionHandler(pelaaja, "luoti", LuotiOsuma); //Pelaaja osuu luotiin
            AddCollisionHandler(pelaaja, "PickUp", PickUp); //Pelaaja osuu PickUpiin eli expaa antavaan kuutioon.
            AddCollisionHandler(pelaaja, "salaisuus", delegate { kentta = 0; AloitaPeli(); }); // =O
            Add(pelaaja);
            Camera.Follow(pelaaja); //Kamera seuraa pelaajaa eikä mene kentän yli eli näytä tyhjyyttä
            Camera.Zoom(0.8);
            Camera.StayInLevel = true;
        }

        /// <summary>
        /// Aliohjelma, joka käynnistää timerin mikä palauttaa Staminaa 3 pistettä per 0,1s eli 30p sekunnissa.
        /// Prosentuaalista palautusta kokeilin, mutta huomasin että rikkoo pelin myöhemmin, koska pelaaja voi hyväksikäyttää fysiikoita ja hyppiä kentän vaikeiden kohtien yli.
        /// </summary>
        private void StaminaPalautus()
        {
            Timer rStamina = new Timer(); //Mittarin määreet
            rStamina.Interval = 0.1;
            rStamina.Timeout += delegate { pelaajaStamina.Value += 3; }; //Nostaa staminaa 3p per 0,1s
            rStamina.Start();
        }

        /// <summary>
        /// Aliohjelma, joka luo näppäimet peliin.
        /// </summary>
        private void LuoNappaimet()
        {
            Vector suuntaYlos = new Vector(0, 600); //Liikevektorit, joita käytetään
            Vector liikkuminen = new Vector(900, 0);
            PhoneBackButton.Listen(ConfirmExit, "Lopeta peli");
            Keyboard.Listen(Key.Space, ButtonState.Pressed, Hyppy, "Hyppää", pelaaja, suuntaYlos); //käytetään määritettyjä vektoreita hyppyyn ja liikkeeseen
            Keyboard.Listen(Key.D, ButtonState.Down, LiikutaPelaajaa, "Liikkuu oikealle", liikkuminen);
            Keyboard.Listen(Key.A, ButtonState.Down, LiikutaPelaajaaMiinus, "Liikkuu vasemmalle", -liikkuminen);
            Keyboard.Listen(Key.Right, ButtonState.Pressed, AmmusOikea, "Ammus");
            Keyboard.Listen(Key.Left, ButtonState.Pressed, AmmusVasen, "Ammus");
            if (ronja == false)
            {
                Keyboard.Listen(Key.E, ButtonState.Pressed, Taika, "Taika");
                Keyboard.Listen(Key.Q, ButtonState.Pressed, SpelliVaihto, "Spelli");
            }          
            Keyboard.Listen(Key.F, ButtonState.Pressed, Dash, "Hyökkäys");
            Keyboard.Listen(Key.P, ButtonState.Pressed, Jumala, "Debug"); //Tätä ei ole tarkoitus kertoa avoimesti kaikille
            Keyboard.Listen(Key.T, ButtonState.Pressed, Testinappula, "Testaa haluttua ominaisuutta"); //Tällä testataan aliohjelmia
            Keyboard.Listen(Key.Escape, ButtonState.Pressed, PalataankoValikkoon, "Palaa päävalikkoon");
        }

        /// <summary>
        /// Aliohjelma, joka luo tekstikentän oikeaan ylälaitaan. Tämä toimii tilannekatsauksena ja kertoo mitä pelaajalle tapahtuu pelissä
        /// Tähän pitää luoda viela mahdollisuus vaihtaa valittua spelliä napin painalluksella. Huom. lisää luonäppäimiä.
        /// </summary>
        /// <param name="x">X-koordinaattiin</param>
        /// <param name="teksti">Teksti joka näytetään oikeassa yläkulmassa</param>
        private void SpelliVaihto()
        {
            if (valinta == spellit - 1)
            {
                valinta = 0;
            }
            else
            {
                valinta++;
            }

            spelli = new Label(120, 20); //Tekstikentän määrittelyä
            spelli.Text = spellNimi[valinta];
            spelli.X = Screen.Left + 80;
            spelli.Y = Screen.Top - 165;
            spelli.BorderColor = Color.Transparent;
            spelli.Color = Color.Transparent;
            Add(spelli);
            Timer tekstikentta = new Timer(); //Tekstikenttä näkyy vain tietyn aikaa ylhäällä, koska miksi säilyttää vanha tieto, tosin olishan se mediaseksikästä nähdä HP + 1 koko ajan
            Timer.SingleShot(1.0, delegate { spelli.Destroy(); });
            tekstikentta.Start();
        }

        /// <summary>
        /// Aliohjelma, joka kysyy haluaako pelaaja varmasti palata päävalikkoon. Default esc sammuttaa pelin, mikä on huono asia.
        /// </summary>
        private void PalataankoValikkoon()
        {
            MultiSelectWindow PaavalikkoPaluu = new MultiSelectWindow("Palataanko valikkoon?",
            "Kyllä", "Ei");
            Add(PaavalikkoPaluu);
            PaavalikkoPaluu.AddItemHandler(0, LuoAlkuValikko); //Kyllä > palaa valikkoon
            PaavalikkoPaluu.AddItemHandler(1, null); //Ei -> ei tapahdu mitään
        }

        /// <summary>
        /// Aliohjelma, joka luo "koodikentän" mihin voi syöttää "koodeja", joista tapahtuu jotain.
        /// </summary>
        private void Jumala()
        {
            InputWindow koodit = new InputWindow("?"); //Mysteerit on kivoja.
            koodit.TextEntered += KoodiKentta; //Syötetty koodi menee koodiKentta aliohjelmaan
            Add(koodit);
        }

        /// <summary>
        /// Aliohjelma, joka tulkkaa annettuja koodeja tapahtumiksi
        /// </summary>
        /// <param name="ikkuna">Koodiksi syötetty pätkä</param>
        private void KoodiKentta(InputWindow ikkuna)
        {
            string koodi = ikkuna.InputBox.Text;
            if (koodi == "iddqd") //Haha doom reference. Tämä tekee pelaajan kuolemattomaksi (HP voi silti tippua, mutta 0HP ei johda kuolemaan) ja pelaaja voi mennä suoraan maaliin.
            {
                tgm = true; //Tekee kuolemattomaksi, vastaava muuttuja tavataan kuoleman käsittelevässä aliohjelmassa
                maali = true; //Tämä mahdollistaa maaliin menon ilman että pelaaja kerää tarvittavan määrän nappuloita
                TilanneKatsaus(200, "Olet kuolematon!");//Teksti menee aliohjelmaan joka printtaa tekstin ylälaitaan
            }
            else if (koodi == "idkfa") //Ja toinen Doom reference. Tämä nostaa kaikki stat arvot täyteen, mutta nosta pelaajan leveliä. Hard Coded kattojen takia pelaaja ei voi ylittää rajoja
            {
                pelaajaHP.MaxValue = maxHP; //Pelaajan HP = Max HP.
                pelaajaHP.Value = maxHP;    //Täyttää pelaajan HP palkin
                LuoPalkkiHP();      //Luo palkin uudestaan
                pelaajaMP.MaxValue = maxMP; //Sama homma mitä ylempänä mutta kaikkiin seuraaviin.
                pelaajaMP.Value = maxMP;
                LuoPalkkiMP();
                pelaajaStamina.MaxValue = maxStamina;
                pelaajaStamina.Value = maxStamina;
                LuoPalkkiStamina();
                TilanneKatsaus(200, "UNLIMITED POWER!"); //Teksti menee aliohjelmaan joka printtaa tekstin ylälaitaan
            }
            else if (koodi == "fuckthisshit") //Tämä oli tombaa:n hauska idea, muutin vain alkuperäistä ideaa joka oli tämä koodi mutta jumalamode. Pelaaja pääsee kentän läpi automaattisesti.
            {
                maali = true; //Pelaaja pääsee maaliin, mutta tällä ei ole väliä alemman pätkän takia. Tämä on vain failsafe.
                MaaliinMeno(pelaaja, isoVihu); //Luo ikkunan, joka kysyy haluaako pelaaja mennä seuraavaan kenttään vai palata menuun.
            }
            else if (koodi == "idgaf") //Koodi joka ns. suorittaa kaikki kentät automaattisesti eli pelaaja voi tästä lähin valita vapaasti kentät 1-3, vaikka ei pääsisi ekaa kenttää läpi.
            {
                for (int i = 1; i < kentatLapi.Length; i++) //Silmukka joka muuttaa kaikki kentät läpäistyiksi.
                {
                    kentatLapi[i] = true;
                    arvosanat[i] = 5; // 5/5 suoritus veliseni
                }
                TilanneKatsaus(250, "Olet oman tien kulkija");//Teksti menee aliohjelmaan joka printtaa tekstin ylälaitaan, tarviiko tätä sanoa useasti?
            }
        }

        /// <summary>
        /// Tähän voi itse määrittää mitä nappula tekee ja testata vapaasti pelissä. Pitäis varmaan disablettaa
        /// </summary>
        private void Testinappula()
        {
            //Tässä voi testata miten ominaisuudet toimii ilman, että joutuu säheltämään oikeaan paikkaan joka kerta
            expa.Value += 100 * leveli;
        }

        /// <summary>
        /// Aliohjelma, joka liikuttaa pelaajaa syötetyn vektorin verran ja määrittää pelaajan suunnaksi oikean.
        /// </summary>
        /// <param name="vektori">Vektori mihin suuntaan liikutaan</param>
        private void LiikutaPelaajaa(Vector vektori)
        {
            if (ronja == true)
            {
                vektori = vektori * 1.2;
            }
            suuntaOikea = true; //Pelaajan suunta on nyt vasen, tämä on visuaaliseen tarkoitukseen + ns. dashiin, että pelaaja dashaa oikeaan suuntaan.
            pelaaja.Push(vektori);
        }

        /// <summary>
        /// Aliohjelma, joka liikuttaa pelaajaa syötetyn vektorin verran ja määrittää pelaajan suunnaksi vasemman.
        /// </summary>
        /// <param name="vektori">Suunta mihin liikutaan</param>
        private void LiikutaPelaajaaMiinus(Vector vektori)
        {
            suuntaOikea = false; //Pelaajan suunta on nyt vasen, tämä on visuaaliseen tarkoitukseen + ns. dashiin.
            pelaaja.Push(vektori);
        }

        /// <summary>
        /// Aliohjelma, joka pistää pelaajan hyppäämään
        /// </summary>
        /// <param name="pelaaja">Pelaajan hahmo, jonka on tarkoitus hypätä</param>
        /// <param name="suunta">Vektori johon pelaaja hyppää eli siis ylöspäin</param>
        private void Hyppy(PhysicsObject pelaaja, Vector suunta)
        {
            if (ronja == true)
            {
                suunta = suunta * 1.2;
            }
        int hyppyStamina = 40; //Voidaan määrittää hypyn vaativuus, 40 koska default latausnopeus 30 per sekunti, eli 1,4s välein voi hypätä uudelleen jos staminat vähissä
            if (voiHypata == true && pelaajaStamina.Value > hyppyStamina) //Tämä varmistaa että pelaaja ei ole jo hypännyt ja että pelaajalla on tarpeeksi puhtia hyppyyn
            {
                voiHypata = false; //Tämä estää uuden hypyn kunnes pelaaja törmää johonkin (Tormays aliohjelma), hyvällä ajoituksella pelaaja voi wall-jumpata tai triple-jumpata
                pelaajaStamina.Value -= hyppyStamina; //Hyppy vie 40 staminaa, voi säätää.
                pelaaja.Hit(suunta); //Hyppää vektorin verran (ylös)
            }
        }

        /// <summary>
        /// Aliohjelma, joka mahdollistaa pelaajan uuden hypyn
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="taso">Tasojen fysiikkaolio</param>
        private void Tormays(PhysicsObject pelaaja, PhysicsObject taso)
        {
            voiHypata = true; //Pelaaja voi hypätä kun törmää johonkin tasoon, teknisesti toimii myös vihollisiin törmätessä, "It's not a bug, it's a feature"
        }

        /// <summary>
        /// Aliohjelma, joka suorittaa "potionin" käytön
        /// </summary>
        private void Taika()
        {

            if (valinta == 0)
            {
                if (pelaajaMP.Value >= 1 && pelaajaHP.Value < pelaajaHP.MaxValue) //Tarkistaa onko pelaajalla potioneja käytettävissä ja onhan pelaajan HP alle maksimin, että jotain hyötyä
                {
                    pelaajaHP.Value += pelaajaHP.MaxValue / 2; //Palauttaa pelaajalle tämän hetkisestä max hp:sta puolet HP:hen
                    pelaajaMP.Value -= 1; //MP tippuu yhdellä koska yksi potioni käytetty
                }
            }
            else if (valinta == 1)
            {
                //ei ole vielä
            }
        }

        /// <summary>
        /// Aliohjelma, joka avaa maalin kun pelaajan on kerännyt tarpeeksi nappuloita
        /// </summary>
        private void MaaliAuki()
        {
            TilanneKatsaus(300, "Vainu löydetty, jatka matkaa"); //Tekstikenttä oikealla ylhäällä
            maali = true; //Aktiivoi maaliin menon eli bossi voi spawnata myös.
        }

        /// <summary>
        /// Aliohjelma, joka käsittelee maaliin menon.
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="maali">maalin fysiikkaolio</param>
        private void MaaliinMeno(PhysicsObject pelaaja, PhysicsObject maaali) //Vikaan kenttään vois tehdä eri hommelin
        {
            if (maali == true) //Jos pelaaja on kerännyt 10 nappulaa (tai käyttänyt TGM-koodi), tämä toteutuu ja pelaaja voi päästä kentän läpi
            {
                kentatLapi[kentta] = true; //Tämän hetkinen kenttä suoritetaan taulukossa.
                kentta++; //Nostetaan varmuudeksi kenttäindeksi koska halutaan seuraavaan kenttään.
                if (kentta == kenttiaPelissa || kentta == 1)
                {
                    MultiSelectWindow maaliValikko = new MultiSelectWindow("Pääsit pelin loppuun! Onneksi olkoon!",
                    "Palaa päävalikkoon", "Lopeta peli");
                    Add(maaliValikko);
                    maaliValikko.AddItemHandler(0, LuoAlkuValikko); //Palaa päävalikkoon
                    maaliValikko.AddItemHandler(1, ConfirmExit); //Sulkee pelin kokonaan, miksi kukaan haluaisi tätä painaa?
                }
                else
                {
                    MultiSelectWindow maaliValikko = new MultiSelectWindow("Pääsit kentän loppuun",
                    "Jatka toiseen kenttään", "Palaa päävalikkoon", "Lopeta peli");
                    Add(maaliValikko);
                    maaliValikko.AddItemHandler(0, AloitaPeli); //Siirtyy seuraavaan kenttään
                    maaliValikko.AddItemHandler(1, LuoAlkuValikko); //Palaa päävalikkoon
                    maaliValikko.AddItemHandler(2, ConfirmExit); //Sulkee pelin kokonaan, miksi kukaan haluaisi tätä painaa?

                }
            }
            else
            {
                TilanneKatsaus(400, "Tämä ei välttämättä ole oikea suunta..."); //Tämä on vain tämmöinen bullshit este että pääsee maaliin ilman nappuloita, ei voi kyl tapahtua normisti
            }
        }

        /// <summary>
        /// Aliohjelmaa, joka suorittaa "dashin" toiminnan. Jos pelaajan stamina on tarpeeksi, niin pelaaja voi dashata vihuihin. Pikkuvihut kuolee yhdestä mutta bossit ei.
        /// </summary>
        private void Dash()
        {
            int dashStamina = 60;
            if (pelaajaStamina.Value > dashStamina) //Pelaaja tarvii puhtia että voi dashata ja tehdä vahinkoa
            {
                pelaaja.Color = Color.Red; //Pelaaja näkee milloin tekee damagea, punaisena damagea ja oranssina ei
                voiVahingoittaa = true; //Tämä aktivoi muuttujan, jossa muut ohjelmat tietää että pelaaja dashaa eli voi tehdä damagea
                pelaajaStamina.Value -= dashStamina; //Pelaajan stamina laskee dashin kuluttaman määrän, tämä estää loputtoman ketjuttamisen
                Vector pelaajanSuunta; //Kumpaan suuntaan pelaaja lentää
                if (suuntaOikea == true) //Jos pelaaja katsoo oikealle niin dash oikealle. Tästä ollut puhetta aikaisemmin
                {
                    pelaajanSuunta = Vector.FromLengthAndAngle(1000.0, pelaaja.Angle); //Vektorin määritys
                }
                else //kun taas jos pelaajan suunta vasen niin dashaa vasemmalle.
                {
                    pelaajanSuunta = Vector.FromLengthAndAngle(-1000.0, pelaaja.Angle);
                }
                pelaaja.Hit(pelaajanSuunta);
                Timer.SingleShot(0.5, delegate { pelaaja.Color = Color.Orange; voiVahingoittaa = false; }); //Pelaaja tekee damagea 0,5sec ajan, minkä jälkeen ei voi tehdä damagea
            }
        }

        /// <summary>
        /// Aliohjelma joka käsittelee tilanteen kun pelaajan HP on 0 (ilman tgm) ja pelaaja kuolee.
        /// </summary>
        private void PeliOhi()
        {
            if (tgm == false) //Jos pelaaja ei ole kuolematon niin tämä tapahtuisi kuollessa
            {
                pelaaja.Destroy(); //Pelaajan hahmo katoaa
                MediaPlayer.Stop();
                MediaPlayer.Play("Piano");

                Label kuolema = new Label(200, 40, "Sinä kuolit!"); //Luodaan teksti joka näkyy punaisella keskellä näyttöä
                kuolema.X = 0;
                kuolema.Y = 0;
                kuolema.TextColor = Color.Red;
                kuolema.Font = Font.DefaultLargeBold;
                Add(kuolema);

                AlustaMuuttujat(); //Alustaa muuttujat kokonaan eli peli resetöityy, mitäs olit niin huono pelaaja
                Timer.SingleShot(5.0, LuoAlkuValikko); //Luo alkuvalikon, kun pelaaja on joutunut katsomaan omaa surkeuttaan 5sec.
            }
        }

        /// <summary>
        /// Aliohjelma joka luo vihollisen kenttätiedoston määrittelemään paikkaan
        /// </summary>
        /// <param name="paikka">Paikka mihin vihollinen spawnaa</param>
        /// <param name="x">Ei käytössä</param>
        /// <param name="y">Ei käytössä</param>
        private void LuoVihu(Vector paikka, double x, double y)
        {
            PhysicsObject vihu = new PhysicsObject(50, 50); //Vihun määreet
            vihu.Position = paikka;
            vihu.Shape = Shape.Circle;
            vihu.Color = Color.Red;
            vihu.Tag = "vihollinen"; //Tätä käytetään että tunnistetaan törmäys pelaajan kanssa
            vihu.CanRotate = true;
            vihu.IgnoresGravity = true; //Leijuu
            AddCollisionHandler(vihu, "ammus", AmmusOsuma); //Pelaaja osuu viholliseen
            Add(vihu);

            FollowerBrain vihuAI = new FollowerBrain(pelaaja); //Tekoäly. Vihollinen seuraa pelaajaa kunnes törmää pelaajaan
            vihuAI.Speed = 200;
            vihuAI.DistanceFar = 800;
            vihuAI.DistanceClose = 0;
            vihuAI.StopWhenTargetClose = false;
            vihu.Brain = vihuAI;
        }

        /// <summary>
        /// Aliohjelma, joka luo bossille luotisade tempun, eli luoteja lentää jokaiseen suuntaan.
        /// </summary>
        private void LuotiSade()
        {
            if (bossiRajoitin == false) //Ilman tätä rajoitinta homma meni rikki ja kuulia alkoi lentämään ihan naurettavan paljon, tehden pelistä entistä sadistisemman
            {
                for (int i = 0; i < kentta * 5 + 5; i++) //Luo 20 palloa bossin keskelle, mitkä lentävät random suuntiin luoden illuusion että ne lentävät tasaisesti ympärille
                {
                    LuoLuoti(isoVihu.X, isoVihu.Y); //Spawnaa luodit bossin keskelle
                    bossiRajoitin = true; //Tämä varmistaa että bossi ei luo tätä uudelleen, ennen tätä bossi saattoi luoda 100 luotia ennen kuin aliohjelma terminoi ittensä.
                }
            }
            Timer.SingleShot(2.0, delegate { bossiRajoitin = false; }); //Tämä mahdollistaa luotiSateen uudelleen käytön kun aika koittaa, ilman tätä bossi oli naurettavan vaikea.
        }

        /// <summary>
        /// Aliohjelma, joka luo luoteja mitä bossi sylkee.
        /// </summary>
        /// <param name="x">X koordinaatti</param>
        /// <param name="y">Y koordinaatti</param>
        private void LuoLuoti(double x, double y)
        {
            PhysicsObject luoti = new PhysicsObject(20, 20); //Bossin luodin määreet
            luoti.Shape = Shape.Circle;
            luoti.Color = Color.Red;
            luoti.X = x;
            luoti.Y = y;
            luoti.Restitution = 0; //Estää kimpoilun, paitsi että ei estä todellisuudessa koska korkea nopeus
            luoti.CollisionIgnoreGroup = 1; //Estää törmäilyn bossiin
            luoti.LifetimeLeft = TimeSpan.FromSeconds(1.5); //Luoti lentää 1,5sec kimpoillen
            luoti.Tag = "luoti"; //Törmäystunnistustagi
            luoti.CanRotate = true;
            luoti.IgnoresGravity = true;
            Add(luoti);

            RandomMoverBrain luotiAI = new RandomMoverBrain(3000); //Tekoäly, joka laittaa luodin lentämään random suuntaan älytöntä nopeutta
            luoti.Brain = luotiAI;
        }

        private void LuoPommi(double x, double y)
        {
        PhysicsObject pommi = new PhysicsObject(15, 15);
        pommi.Shape = Shape.Circle;
        pommi.Color = Color.Black;
        pommi.X = x;
        pommi.Y = y;
        pommi.Restitution = 0.0;
        pommi.LinearDamping = 0.9;
        Add(pommi);
        }

        /// <summary>
        /// Aliohjelma, joka luo luoteja mitä bossi sylkee.
        /// </summary>
        /// <param name="x">X koordinaatti</param>
        /// <param name="y">Y koordinaatti</param>
        private void AmmusOikea()
        {
            if (ammusRajoitin == true || pelaajaStamina.Value < 15)
            {
                return;
            }
            PhysicsObject ammus = new PhysicsObject(10, 10); //Bossin luodin määreet
            ammus.Shape = Shape.Circle;
            ammus.Color = Color.Black;
            ammus.X = pelaaja.X;
            ammus.Y = pelaaja.Y;
            ammus.Restitution = 0; //Estää kimpoilun, paitsi että ei estä todellisuudessa koska korkea nopeus
            ammus.LifetimeLeft = TimeSpan.FromSeconds(1.5); //Luoti lentää 1,5sec kimpoillen
            ammus.CollisionIgnoreGroup = 2;
            ammus.Tag = "ammus"; //Törmäystunnistustagi
            ammus.CanRotate = true;
            ammus.IgnoresGravity = true;
            Add(ammus);

            Vector suunta = Vector.FromLengthAndAngle(1000.0, pelaaja.Angle);
            ammus.Hit(suunta);
            ammusRajoitin = true;
            Timer.SingleShot(0.2, delegate { ammusRajoitin = false; });
            pelaajaStamina.Value -= 15;
        }

        /// <summary>
        /// Aliohjelma, joka luo luoteja mitä bossi sylkee.
        /// </summary>
        /// <param name="x">X koordinaatti</param>
        /// <param name="y">Y koordinaatti</param>
        private void AmmusVasen()
        {
            if (ammusRajoitin == true || pelaajaStamina.Value < 15)
            {
                return;
            }
            PhysicsObject ammus = new PhysicsObject(10, 10); //Bossin luodin määreet
            ammus.Shape = Shape.Circle;
            ammus.Color = Color.Black;
            ammus.X = pelaaja.X;
            ammus.Y = pelaaja.Y;
            ammus.Restitution = 0; //Estää kimpoilun, paitsi että ei estä todellisuudessa koska korkea nopeus
            ammus.CollisionIgnoreGroup = 2; //Estää törmäilyn bossiin
            ammus.LifetimeLeft = TimeSpan.FromSeconds(1.5); //Luoti lentää 1,5sec kimpoillen
            ammus.Tag = "ammus"; //Törmäystunnistustagi
            ammus.CanRotate = true;
            ammus.IgnoresGravity = true;
            Add(ammus);

            Vector suunta = Vector.FromLengthAndAngle(-1000.0, pelaaja.Angle);
            ammus.Hit(suunta);
            ammusRajoitin = true;
            Timer.SingleShot(0.2, delegate { ammusRajoitin = false; });
            pelaajaStamina.Value -= 15;
        }

        /// <summary>
        /// Aliohjelma joka luo tykkejä "Hidden bossin" kenttään
        /// </summary>
        /// <param name="paikka">Koordinaatit</param>
        /// <param name="x">Koko</param>
        /// <param name="y">Koko</param>
        private void LuoTykki(Vector paikka, double x, double y)
        {
            PhysicsObject tykki = PhysicsObject.CreateStaticObject(x, y); //Määreet
            tykki.Position = paikka;
            tykki.Shape = Shape.Rectangle;
            tykki.Color = Color.DarkGray;
            tykki.CollisionIgnoreGroup = 1; //Ei törmää bossiin tai muihin luoteihin
            tykki.IgnoresGravity = true;
            tykki.IgnoresPhysicsLogics = true;
            Add(tykki);

            Timer Ampuminen = new Timer(); //Ampuu luoteja 1s välein
            Ampuminen.Interval = 1.0;
            Ampuminen.Timeout += delegate { LuoLuoti(tykki.X, tykki.Y); }; //Tekee luodin
            Ampuminen.Start();
        }

        /// <summary>
        /// Aliohjelma, joka käsittelee pelaajan törmäyksen viholliseen
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="vihu">Vihu</param>
        private void VihuOsuma(PhysicsObject pelaaja, PhysicsObject vihu)
        {
            Vector osuma = new Vector(-500, 500); //Pelaaja lentää tämän verran
            if (voiVahingoittaa == true) //Jos pelaaja voi vahingoittaa vihuja (eli dash on päällä), niin tämä tapahtuu
            {
                CollisionHandler.DestroyObject(vihu, pelaaja); //Vihollinen kuolee pelaajan törmäyksestä
                expa.Value += LaskeExpa(); //Pelaajan saamat expat vihun tappamisesta
            }
            else if (damageSuoja == true) //Jos pelaaja on ottanut damagea, niin pelaaja ei ota uudestaan damagea ennen kuin suoja on pois päältä
            {
                pelaaja.Hit(osuma); //Pelaaja lentää mutta ei ota damagea
            }
            else
            {
                pelaajaHP.Value -= 1; //Pelaaja menettää 1HP jos ei ole damage suojaa eikä ole dashaamassa
                osumat++;
                pelaaja.Hit(osuma); //Lentoon
                damageSuoja = true; //Pelaajalle aktivoituu damage suoja, jotta pelaaja ei ota heti uudestaan damagea
                pelaaja.Color = Color.AshGray; //Pelaajan väri muuttuu, jotta pelaaja tietää olevansa suojassa damagelta hetken
                Timer.SingleShot(1.5, delegate { damageSuoja = false; pelaaja.Color = Color.Orange; }); //Tämä poistaa pelaajan damage suojan 1,5s osuman jälkeen
            }
        }

        private void AmmusOsuma(PhysicsObject ammus, PhysicsObject vihu)
        {
            CollisionHandler.DestroyObject(vihu, ammus);
            CollisionHandler.DestroyObject(ammus, vihu);
            expa.Value += LaskeExpa();
        }

        /// <summary>
        /// Tämä funktio laskee pelaajan saamat expat vihun tappamisesta. Kenttä vaikuttaa expan saamismäärään.
        /// </summary>
        /// <returns>Palauttaa expan määrän</returns>
        /// <example>
        /// <pre name="test">
        /// kentta = 1;
        /// leveli = 5;
        /// LaskeExpa === 9;
        /// </pre>
        /// </example>
        private double LaskeExpa()
        {
            if (kentta == 1) //Kenttä vaikuttaa expan määrään
            {
                return 4 + (leveli); //Ykköskentän expan määrät. Kasvaa tylsästi eli eli level 2 pelaaja saa 6 expaa per vihu, kun taas level 15 pelaaja saa 19 expaa.
            }
            else if (kentta == 2)
            {
                return leveli * 2 - 1; //Tämä on vähän erilainen kasvutyyli. Lukijat varmaan huomaavat että level 4 pelaaja saa huonommin expaa kuin kentässä 1, mutta tämän ei pitäisi tapahtua.
            }
            else //kentän 3 ja kröhsalaisenkentänkröh expamäärä.
            {
                return leveli * 3 - 9; //Tämäkin on erikoinen. Level 1-2 pelaajat jäisivät miinuksille, ei pitäisi tapahtua. Level 7 tienaa vasta enemmän kuin kenttässä 1 ja level 9 kenttä 2. 
            }
        }

        /// <summary>
        /// Aliohjelma, joka käsittelee luodin osuman pelaajaan
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="luoti">Osuva luoti</param>
        private void LuotiOsuma(PhysicsObject pelaaja, PhysicsObject luoti)
        {
            Vector osuma = new Vector(-300, 300); //Lennon voimakkuus
            if (damageSuoja == false) //jos damage suojaa ei ole niin tämä tapahtuu
            {
                pelaajaHP.Value -= 1; //Damage
                osumat++;
                damageSuoja = true; //Damage suoja päälle että ei ota heti damagea
                pelaaja.Color = Color.AshGray; //Visuaalinen efekti suojalle
                Timer.SingleShot(1.5, delegate { damageSuoja = false; pelaaja.Color = Color.Orange; }); //Poistetaan suojaus ja visuaalinen avustin
            }
            pelaaja.Hit(osuma); //Lentoon
            CollisionHandler.DestroyObject(luoti, pelaaja); //Luoti katoaa osuman jälkeen
        }

        /// <summary>
        /// Aliohjelma, joka käsittelee bossin aloituksen
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="entry">Bossi-tappelun aloitusruutu</param>
        private void BossinAlku(PhysicsObject pelaaja, PhysicsObject entry)
        {
            if (maali == true) //Jos pelaaja on kerännyt nappulat niin bossin voi aktivoida
            {
                if (kentta > 0)
                {
                    LuoPalkkiBossHP(40 * kentta - (10 - 3 * kentta)); //Kutsuu aliohjelman, joka luo bossille uhkaavan HP-palkin ja nimen, antaa parametrina lasketun HP:n
                }
                else
                {
                    LuoPalkkiBossHP(100);
                }
                CollisionHandler.DestroyObject(entry, pelaaja); //Tuhoaa ruudun, koska muuten tämä jäisi kummittelemaan ja resettämään taistelua
            }
            else //Jos ei tarpeeksi nappuloita tai koodeja käytössä
            {
                Vector tyonto = new Vector(1000, 0); //Puskee pelaajan pois
                pelaaja.Hit(tyonto);
                TilanneKatsaus(420, "Mystinen voima estää sinua menemästä"); //Olen todella tylsä, tiedän.
            }
        }

        /// <summary>
        /// Aliohjelma, joka luo bossille HP-palkin ja kutsuu myös ohjelman joka luo tekstin
        /// </summary>
        /// <param name="arvo">Bossin HP:n määrä, joka on aliohjelmakutsussa määritelty</param>
        private void LuoPalkkiBossHP(double arvo)
        {
            bossHP = new DoubleMeter(arvo); //Bossin HP-mittarin määritys
            bossHP.MaxValue = arvo; //Bossin HP:n määrä, pelaajan damageen on oma laskin muualla
            bossHP.LowerLimit += BossKill; //Kutsuu bossin kuolemisen käsittelijän

            ProgressBar palkkiBossHP = new ProgressBar(700, 26); //Bossin HP palkin määreet
            palkkiBossHP.X = Screen.Left + 510;
            palkkiBossHP.Y = Screen.Bottom + 38;
            palkkiBossHP.BarColor = Color.Red;
            palkkiBossHP.BorderColor = Color.Black;
            palkkiBossHP.Color = Color.Gray;
            palkkiBossHP.BindTo(bossHP);
            Add(palkkiBossHP);
            LuoBossTeksti(bossTeksti[kentta], bossNimi[kentta]); //Aliohjelmakutsu joka luo bossin tekstin.

            luodit = new Timer(); //Luo timerin joka syöksee bossista luoteja.
            luodit.Interval = 4.3 - 0.2 * kentta; //Kentän mukaan vaikeutuu hulluna esimerkkinä kentässä 21 lentäisi luotisade 0,1s välein ja luoteja olisi 105 per aalto
            luodit.Timeout += LuotiSade; //Tekee luotisateen
            luodit.Start();
        }

        /// <summary>
        /// Aliohjelma, joka luo bossille näkyvän nimen HP-palkin päälle
        /// </summary>
        /// <param name="x">Kuinka leveä tekstin täytyy olla, jotta se mahtuu sopivasti palkin päälle</param>
        /// <param name="nimi">Bossin nimi, joka määritetään aliohjelmakutsussa</param>
        private void LuoBossTeksti(double x = 325, string nimi = "Daeusnoum, Kaaoksesta syntynyt") //Default on kentän 1 bossin nimi
        {
            Label nimiBoss = new Label(x, 40.0); //Tekstikentän määritykset
            nimiBoss.X = Screen.Left + 150 + x / 2; //X määrittää mistä tekstikenttä lähdetään luomaan. Pidemmät nimet tarvitsevat pidemmälle ulottuvan tekstikentän alueen.
            nimiBoss.Y = Screen.Bottom + 70;
            nimiBoss.TextColor = Color.Black;
            nimiBoss.Text = nimi; //Aliohjelmakutsun määrittämä nimi bossille, joka tulee näkyviin.
            Add(nimiBoss);
        }

        /// <summary>
        /// Aliohjelma joka luo bossin kenttätiedoston määrittämään kohtaan. Tämä on vain bossille 1.
        /// </summary>
        /// <param name="paikka">Paikka mihin bossi spawnaa</param>
        /// <param name="x">Ei käytetty</param>
        /// <param name="y">Ei käytetty</param>
        private void LuoIsoVihu(Vector paikka, double x, double y)
        {
            if (kentta > 0)
            {
                isoVihu = new PhysicsObject(300, 300); //Määritykset
                isoVihu.Color = Color.DarkRed;
                isoVihu.CanRotate = true;
            }
            else
            {
                isoVihu = PhysicsObject.CreateStaticObject(300, 150);
                isoVihu.Color = Color.Orange;
                isoVihu.CanRotate = false;
            }
            isoVihu.Position = paikka;
            isoVihu.Shape = Shape.Circle;
            isoVihu.Tag = "bossi"; //Eri tagi vihuista
            isoVihu.CollisionIgnoreGroup = 1; //Tämä estää luotien ja bossien yhteentörmäyksen
            isoVihu.IgnoresGravity = true;
            AddCollisionHandler(isoVihu, "ammus", AmmusBossi); //Ammus osuu bossiin
            Add(isoVihu);
            if (kentta > 0)
            {
                RandomMoverBrain isoVihuAI = new RandomMoverBrain(100); //Bossin tekoäly, random liikehdintää yhteen suuntaan kunnes 2s välein vaihtaa suuntaa
                isoVihuAI.ChangeMovementSeconds = 2;
                isoVihu.Brain = isoVihuAI;
            }
        }

        /// <summary>
        /// Aliohjelma, joka käsittelee bossin ja pelaajan törmäyksen
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="isoVihu">Bossin fysiikkaolio</param>
        private void BossiOsuma(PhysicsObject pelaaja, PhysicsObject isoVihu)
        {
            if (voiVahingoittaa == false) //Voiko pelaaja tehdä vahinkoa bossiin eli ei voi. Pelaaja ottaa törmäyksestä vahinkoa itse.
            {
                Vector osuma = new Vector(-800, 800); //Lentoon lähdön määrittely
                pelaajaHP.Value -= 1; //Vahingon määrä
                osumat++; //Tallennetaan pelaajan ottama osuma, tämä huonontaa pisteytystä kentän lopussa
                pelaaja.Hit(osuma);
                damageSuoja = true; //Pelaajalle damagesuoja toistuvia osumia vastaan
                pelaaja.Color = Color.AshGray; //Visuaalinen avustin että huomaa milloin ottaa vahinkoa
                Timer.SingleShot(1.5, delegate { damageSuoja = false; pelaaja.Color = Color.Orange; }); //Suoja ja visuaalisuus pois
            }
            else //Eli pelaajalla on "vahingoitus" dashaus päällä
            {
                if (bossDamageSuoja == false) //Joten mikäli jos bossilla ei ole damagesuojaa päällä niin tämä tapahtuu, muuten ei tapahdu mitään
                {
                    bossHP.Value -= PelaajaDamage(leveli); //Pelaaja tekee damagea aliohjelman määrittelemän summan verran.
                    bossDamageSuoja = true; //Asettaa bossille damagesuojan vahinkoa vastaan hetkeksi
                    isoVihu.Color = Color.DarkGray; //Muuttaa bossin väriä, jotta pelaaja huomaa sen olevan turvassa hetken
                    if (kentta > 0)
                    {
                        Timer.SingleShot(0.5, delegate { bossDamageSuoja = false; isoVihu.Color = Color.DarkRed; }); //Damagesuoja ja väriefekti pois.
                    }
                    else
                    {
                        Timer.SingleShot(0.5, delegate { bossDamageSuoja = false; isoVihu.Color = Color.Orange; });
                    }
                }
            }
        }

        private void AmmusBossi(PhysicsObject ammus, PhysicsObject isoVihu)
        {
            CollisionHandler.DestroyObject(isoVihu, ammus);
            bossHP.Value -= 1 + 1 * kentta;
            isoVihu.Color = Color.DarkGray;
            Timer.SingleShot(0.5, delegate { isoVihu.Color = Color.Orange; });
        }

        /// <summary>
        /// Aliohjelma joka käsittelee bossin kuoleman ja siihen liittyvät tapahtumat
        /// </summary>
        private void BossKill()
        {
            ClearTimers(); //Poistaa ajastimet, koska muuten bossin ajastin ei suostunut loppumaan.
            StaminaPalautus(); //Palauttaa staminamittarin toiminnan, syynä tähän ClearTimers();
            Timer.SingleShot(1.0, delegate { pelaaja.Color = Color.Orange; voiVahingoittaa = false; }); //Varmistaa että pelaajalle ei jää pysyvää tappomodea päälle
            damageSuoja = false; //Poistaa pelaajalta damagesuojan, koska normaalisti menee timerin kautta. Syynä tähän kerran tapahtunut pelaajan kuolemattomuus kentän vaihdon jälkeen.
            bossDamageSuoja = false; //Tämäkin nyt varmuuden vuoksi

            Label bossVoitto = new Label(300, 40, "Demoni tuhottu!"); //Luo tekstikentän jossa näkyy sydäntä lämmittävä näky, "Demoni tuhottu" ja maailman on parempi paikka
            bossVoitto.X = 0;
            bossVoitto.Y = 100; //Ihan keskellä oleminen oli hitusen huono asia näkyvyyden kannalta.
            bossVoitto.TextColor = Color.MidnightBlue;
            bossVoitto.Font = Font.DefaultLargeBold;
            Add(bossVoitto);
            Timer.SingleShot(5.0, delegate { bossVoitto.Destroy(); }); //Tuhotaan tekstikenttä 5s päästä.

            expa.Value += 100 + (kentta * 100); //Expaa pelaajalle
            double rating = Rating(osumat);
            Timer.SingleShot(6.5, delegate { TilanneKatsaus(320, rating + "* suoritus"); });
            Arvostelu(rating);
            isoVihu.Destroy(); //Poistaa bossin kentästä
        }

        /// <summary>
        /// Aliohjelma joka luo staattisia objekteja. Muut ohjelmat kutsuvat tätä.
        /// </summary>
        /// <param name="paikka">Objektin koordinaatit</param>
        /// <param name="koko">Objektin koko</param>
        /// <param name="muoto">Objektin muoto</param>
        /// <param name="vari">Objektin väri</param>
        /// <param name="tagi">Objektin tagi, tätä käytetään törmäystunnistukseen. Merkittävä vaikutus toimivuuteen. Ei typoja tässä plox</param>
        private void LuoStaattinenObjekti(Vector paikka, double koko, Shape muoto, Color vari, string tagi)
        {
            PhysicsObject objekti = PhysicsObject.CreateStaticObject(koko, koko); //Objektin määreet, kaikki tulevat aliohjelmakutsussa
            objekti.Position = paikka;
            objekti.Shape = muoto;
            objekti.Color = vari;
            objekti.Tag = tagi;
            objekti.IgnoresGravity = true; //Leijuu
            objekti.IgnoresPhysicsLogics = true;
            Add(objekti);
        }

        /// <summary>
        /// Luo kerättäviä nappuloita, jotka edistävät kenttää kun niitä kerätään
        /// </summary>
        /// <param name="paikka">Sijainti</param>
        /// <param name="x">Ei käytetty</param>
        /// <param name="y">Ei käytetty</param>
        private void LuoNappula(Vector paikka, double x, double y)
        {
            LuoStaattinenObjekti(paikka, 35, Shape.Star, Color.Brown, "nappula");
        }

        /// <summary>
        /// Luo paikan jossa mysteeri tapahtuu
        /// </summary>
        /// <param name="paikka">Paikka johon kröhsalainenbossikenttäkröh spawnaa</param>
        /// <param name="x">Ei käytössä</param>
        /// <param name="y">Ei käytössä</param>
        private void LuoSalaisuus(Vector paikka, double x, double y)
        {
            LuoStaattinenObjekti(paikka, 35, Shape.Star, Color.Olive, "salaisuus");
        }

        /// <summary>
        /// Kun pelaaja törmää nappulaan niin edistymispalkki kasvaa, kun kaikki kerätty niin pelaaja voi edetä bossiin ja/tai maaliin.
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="nappula">Nappulan fysiikkaolio</param>
        private void PisteLisaa(PhysicsObject pelaaja, PhysicsObject nappula)
        {
            edistyminen.Value += 1; //Edistymistä palkkiin
            CollisionHandler.DestroyObject(nappula, pelaaja); //Tuhoaa nappulan
        }

        /// <summary>
        /// Aliohjelma joka luo potionin, jonka voi poimia
        /// </summary>
        /// <param name="paikka">Paikka johon potion spawnaa</param>
        /// <param name="x">Ei käytössä</param>
        /// <param name="y">Ei käytössä</param>
        private void LuoPotion(Vector paikka, double x, double y)
        {
            LuoStaattinenObjekti(paikka, 35, Shape.Heart, Color.Red, "potion");
        }

        /// <summary>
        /// Nostaa pelaajan MP määrää, joka on siis potionin käyttämä resurssi. Tämä siis jäänne siitä kun pelissä piti pystyä käyttämään spellejä, mutta ne olivat liian OP
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="potion">Potionin fysiikkaolio</param>
        private void MPpalautus(PhysicsObject pelaaja, PhysicsObject potion)
        {
            pelaajaMP.Value += 1; //Sininen palkki nousee, jee
            CollisionHandler.DestroyObject(potion, pelaaja); //tuhoaa potionin pelistä
        }

        /// <summary>
        /// Aliohjelma, joka luo PowerUpin minkä pelaaja voi kerätä. Tämä nostaa siis expa palkkia ja nimi on muisto ajasta, kun itemit antoivat suoraan PowerUppeja statteihin
        /// </summary>
        /// <param name="paikka">Paikka mihin PowerUp jää</param>
        /// <param name="x">Ei käytössä</param>
        /// <param name="y">Ei käytössä</param>
        private void LuoPickUp(Vector paikka, double x, double y)
        {
            LuoStaattinenObjekti(paikka, 60, Shape.Diamond, Color.Orange, "PickUp");
        }

        /// <summary>
        /// Aliohjelma joka suorittaa powerupin keräyksen
        /// </summary>
        /// <param name="pelaaja">Pelaajan fysiikkaolio</param>
        /// <param name="PowerUp">Powerupin fysiikkaolio</param>
        private void PickUp(PhysicsObject pelaaja, PhysicsObject PowerUp)
        {
            expa.Value += 100 * kentta; //Pelaaja saa expaa lisää, powerup on siis teknisesti harhaanjohtava nimi mutta teknisesti ei koska nostaa pelaajan kokemuspisteitä
            CollisionHandler.DestroyObject(PowerUp, pelaaja); //Tuhoaa powerupin
        }

        /// <summary>
        /// Aliohjelma, joka luo pelin alkuvalikon, johon palataan jatkuvasti.
        /// </summary>
        private void LuoAlkuValikko()
        {
            ClearAll(); //Poistaa kaiken että mikään ei jää kummittelemaan
            kentta = 1; //Määrittää kentäksi 1, koska jos painaa aloita peli niin aloittaa kentästä 1
            MultiSelectWindow alkuValikko = new MultiSelectWindow("Corgi-run",
            "Aloita peli", "Kenttävalikko", "Tilanne", "Lopeta", "Vaihda hahmo");
            Add(alkuValikko);
            alkuValikko.AddItemHandler(0, Kentta1); //Aloita peli aloitta pelin ns. alusta mutta statit pysyvät
            alkuValikko.AddItemHandler(1, KenttaValikko); //Tästä pelaaja voi valita kentän mitä pelata
            alkuValikko.AddItemHandler(2, Tilanne); //Pelaaja voi tarkistaa oman tilanteensa
            alkuValikko.AddItemHandler(3, ConfirmExit); //Pelaaja voi poistua pelistä, mutta miksi kukaan nyt niin haluaisi tehdä, eihän tämä ole niin surkea peli. Eihän? ;____;
        alkuValikko.AddItemHandler(4, delegate { if (ronja == true) { ronja = false; } else { ronja = true; }; LuoAlkuValikko(); }); //Pelaaja voi poistua pelistä, mutta miksi kukaan nyt niin haluaisi tehdä, eihän tämä ole niin surkea peli. Eihän? ;____;
        MediaPlayer.Stop();
        }

        /// <summary>
        /// Alustaa kaikki olennaiset muuttujat pelissä, ihan just in case.
        /// </summary>
        private void AlustaMuuttujat()
        {
            pelaajaHP = new DoubleMeter(10); //Pelaajan Max HP alussa 10
            pelaajaHP.MaxValue = 10;        //Oikea max 20
            pelaajaHP.LowerLimit += PeliOhi; //Kun HP 0 niin pelaaja kuolee

            pelaajaMP = new DoubleMeter(0); //Pelaajan MAX MP alussa on 4, eli voi käyttää ja varastoida 4 potionia.
            pelaajaMP.MaxValue = 4;     //Oikea max 10

            pelaajaStamina = new DoubleMeter(40); //Pelaajan Stamina alkaa puolesta välistä, jotta ei voi spämmiä heti, ei tällä kyllä sen kummempaa väliä ole
            pelaajaStamina.MaxValue = 90;   //Oikea max 150, mutta alussa vain 90

            leveli = 1; //Levelien resetys
            expa = new DoubleMeter(0); //Expa palkki alkaa nollasta
            expa.MaxValue = ExpaVaatimus(leveli + 1); //aliohjelma kutsu, joka määrittää paljon expaa vaaditaan leveliin eli käytännössä lasketaan +1 aina mukaan kutsuun
            expa.UpperLimit += delegate { leveli++; expa.Value = 0; expa.MaxValue = ExpaVaatimus(leveli + 1); StatValinta(); }; //Mitä tapahtuu kun tämä maksimi saavutetaan (leveli)

            suuntaOikea = true; //Määrittää suunnaksi oikean, just in case
            for (int i = 1; i < kentatLapi.Length; i++)
            {
                kentatLapi[i] = false; //Nollaa kenttien suoritukset, oleellinen ekan kerran käynnistäessä + jos kuolee pelissä.
                arvosanat[i] = 0; //Nollaa kenttien arviot
            }
        }

        /// <summary>
        /// Aliohjelma, joka käytännössä luo pelin mitä pelata. UI:t jne.
        /// </summary>
        private void AloitaPeli()
        {
            ClearAll(); //Varmuudeksi poistaa kaiken
            LuoKentta(); //Luo kentän jossa pelataan
            LuoPalkkiHP(); //Luo pelaajan HP-palkin näkyviin
            LuoAikaLaskuri(300 + (kentta * 180)); //480, 660, 840 ovat ajat, jotka kentille 1, 2 ja 3 on. Ajan loppuessa pelaaja häviää mutta ei kuole.
            LuoEdistymispalkki(); //Luo edistymispalkin, josta pelaaja näkee paljon nappuloita kerätty
            LuoPalkkiStamina(); //Luo pelaajan stamina-palkin
            LuoPalkkiMP(); //Luo pelaajan MP palkin
            LuoExpapalkki(); //Luo pelaajan expa palkin
            LuoWidgetti(); //Luo Profiilikuvan näkyviin
            LuoNappaimet(); //Luo näppäimet peliin
            StaminaPalautus(); //Staminan palautuminen toimintaan
            Gravity = new Vector(0.0, -800.0); //Painovoima peliin
            osumat = 0;
            TilanneKatsaus(450, "Seuraa nappuloita selvittääksesi kohtalosi...");
        }

        /// <summary>
        /// Alihojelma joka luo kenttävalikon päävalikkoon
        /// </summary>
        private void KenttaValikko()
        {
            ClearAll();
            MultiSelectWindow kenttaValikko = new MultiSelectWindow("Kenttävalikko",
            "Kenttä 1", "Kenttä 2", "Kenttä 3", "Palaa");
            Add(kenttaValikko);
            kenttaValikko.AddItemHandler(0, Kentta1); //Kenttä 1, 2 ja 3 omat valikot.
            kenttaValikko.AddItemHandler(1, Kentta2);
            kenttaValikko.AddItemHandler(2, Kentta3);
            kenttaValikko.AddItemHandler(3, LuoAlkuValikko); //Takaisin valikkoon
        }

        /// <summary>
        /// Aliohjelma joka käsittelee kenttä 1 valinnan
        /// </summary>
        private void Kentta1()
        {
        kentta = 1; //Kenttä 1 päälle ja
            AloitaPeli(); //Pelin aloitus
        }

        /// <summary>
        /// Aliohjelma joka käsittelee kenttä 2 valinnan
        /// </summary>
        private void Kentta2()
        {
            if (kentatLapi[1] == true) //Jos kenttä 1 on läpi, niin pelaaja voi mennä kenttään 2, muuten ei
            {
            kentta = 2; //Kenttä 2 tulille
                AloitaPeli(); //Ja kentän lataus
            }
            else //Pelaaja ei ole läpäissyt kenttää 1
            {
                KenttaValikko(); //Luodaan kenttävalikko uudestaan näkyviin
                TilanneKatsaus(300, "Et ole vielä näin pitkällä!"); //Ahahahah et ole päässyt kenttää 1 läpi
            }
        }

        /// <summary>
        /// Aliohjelma, joka käsittelee kenttä 3 valinnan. FIXAA TÄMÄ EI OLE TARPEELLINEN
        /// </summary>
        private void Kentta3()
        {
            if (kentatLapi[2] == true) //tarkistaa onko kenttä 2 päästy läpi vai ei
            {
            kentta = 3; //Kenttä 3 tulille
                AloitaPeli(); //Kentän lataus
            }
            else
            {
                KenttaValikko(); //Kenttävalikko uudestaan päälle
                TilanneKatsaus(300, "Et ole vielä näin pitkällä!"); //Niin, turhaan yritit valita sitä kenttää kun et ole vielä päässyt edellistä läpi.
            }
        }

        /// <summary>
        /// Aliohjelma, joka luo päävalikkoon tilannekatsomisikkunan
        /// </summary>
        private void Tilanne()
        {
            ClearAll(); //Kaikki pois
            LuoNelio(500, 600, 800, 250);   //Tämä luo ison neliön taustalle. Tämän päälle tulee muut elementit, jotka näytetään. Tämä on nyt ns. tausta nimellä tästä eteenpäin

            Widget kuvaPelaaja = new Widget(200.0, 200.0, Shape.Rectangle); //Tämä osio luo pelaajan profiilikuvan, joka lätkäistään taustan päälle
            kuvaPelaaja.X = Screen.LeftSafe + 200;
            kuvaPelaaja.Y = Screen.TopSafe - 170;
            kuvaPelaaja.Color = Color.Gray;
            kuvaPelaaja.BorderColor = Color.Black;
            kuvaPelaaja.Image = LoadImage("Profiili"); //Söpö koira, joka ei vaatinut tekijänoikeuksia koska on koira. Minulla on taasen käyttöoikeudet
            Add(kuvaPelaaja);

            LuoPalkkiHP(325, 80, 28, 40); //Luo HP palkin taustan päälle
            if (ronja == false)
        {
            LuoPalkkiMP(325, 180, 28, 40); //Luo MP palkin taustan päälle
        }
            LuoPalkkiStamina(325, 130, 2.8, 40); //Luo stamina palkin taustan päälle
            LuoExpapalkki(620, 230, 256, 30); //Luo expa palkin taustan päälle

            Label StatLevel = new Label(160, 40); //Tämä osio luo kuvan ja tekstin, jossa näytetään pelaajan tämän hetkinen Leveli eli "taso" max 20
            StatLevel.X = Screen.Left + 405;
            StatLevel.Y = Screen.Top - 230;
            StatLevel.Text = "Leveli: " + leveli; //Tämä on se näkyvä teksti
            StatLevel.BorderColor = Color.Black;
            Add(StatLevel);

            LuoNelio(200, 410, 200, 75); //Neliö kentän 1 tilastolle
            LuoNelio(425, 410, 200, 75); //Neliö kentän 2 tilastolle
            LuoNelio(650, 410, 200, 75); //Neliö kentän 3 tilastolle
            LuoTekstiKentta(kentatLapi[1], 1, 200); //Luo tekstikentän Kentän 1 tilastolle, näyttää läpi tai ei. Sama seuraavat 2 mutta eri kenttä
            LuoTekstiKentta(kentatLapi[2], 2, 425);
            LuoTekstiKentta(kentatLapi[3], 3, 650);
            TilanneKatsaus(250, "Paina ESC poistuaksesi");
            if (arvosanat[1] == 5 && arvosanat[2] == 5 && arvosanat[3] == 5)
            {
                Timer.SingleShot(3.1, delegate { TilanneKatsaus(350, "Muinainen pahuus on herännyt..."); });
            }

            Keyboard.Listen(Key.Escape, ButtonState.Pressed, LuoAlkuValikko, "Palaa taaksepäin tilanteessa"); //Paina esc poistuaksesi
        }

        /// <summary>
        /// Tämä aliohjelma luo käyttöliittymään liittyviä neliöitä. Voidaan viljellä missä vaan mutta pääosin käytetty vaan "Tilanne" aliohjelmassa
        /// </summary>
        /// <param name="x">Neliön X-koordinaatti</param>
        /// <param name="y">Neliön Y-koordinaatti</param>
        /// <param name="leveys">Neliön leveys</param>
        /// <param name="korkeus">Neliön korkeus</param>
        private void LuoNelio(double x, double y, double leveys = 200, double korkeus = 150)
        {
            Widget nelio = new Widget(leveys, korkeus, Shape.Rectangle); //Nämä hommat nyt luo sen neliön
            nelio.X = Screen.Left + x;
            nelio.Y = Screen.Bottom + y;
            nelio.Color = Color.LightGray;
            nelio.BorderColor = Color.Black;
            Add(nelio);
        }

        /// <summary>
        /// Tämä aliohjelma luo tekstit onko kenttä päästy läpi vai, nämä näkyy Tilanne aliohjelmassa.
        /// </summary>
        /// <param name="lapi">Onko kenttä läpi vai ei</param>
        /// <param name="luku">Kentän numero, eli kenttä 1, 2 tai 3</param>
        /// <param name="x">X-koordinaatti tekstikentälle</param>
        private void LuoTekstiKentta(bool lapi, int luku, double x)
        {
            Label kenttaLapi = new Label(160, 40); //Nämä määrittävät tekstikenttää
            kenttaLapi.X = Screen.Left + x;
            kenttaLapi.Y = Screen.Bottom + 425;
            if (lapi == true) //Jos kenttä 1/2/3 on läpi, niin tekstikentän arvo on Kenttä 1/2/3 läpi
            {
                kenttaLapi.Text = "Kentta " + luku + " lapi";
            }
            else //Muuten näkyy vain Kentän numero (ja nimi?)
            {
                kenttaLapi.Text = "Kentta " + luku;
            }
            Add(kenttaLapi);
            Label kenttaArvio = new Label(160, 40); //Nämä määrittävät tekstikenttää
            kenttaArvio.X = Screen.Left + x;
            kenttaArvio.Y = kenttaLapi.Y - 30;
            kenttaArvio.Text = arvosanat[luku] + "*";
            Add(kenttaArvio);
        }

        /// <summary>
        /// Aliohjelma, joka luo HP-palkin näytölle.
        /// </summary>
        /// <param name="x">Palkin X-koordinaatti</param>
        /// <param name="y">Palkin Y-koordinaatti</param>
        /// <param name="leveys">HP-Palkin leveys, tämä kasvaa kun MAX HP arvo muuttuu isommaksi levelien myötä. def 14</param>
        /// <param name="korkeus">HP-palkin korkeus, koska vaihtelee tilanne ikkunan ja pelinäkymän välillä def 20</param>
        private void LuoPalkkiHP(double x = 115, double y = 14, double leveys = 1, double korkeus = 8)
        {
            ProgressBar palkkiHP = new ProgressBar(pelaajaHP.MaxValue * leveys, korkeus); //HP-palkin määrittelyä
            palkkiHP.X = Screen.Left + x + (pelaajaHP.MaxValue * leveys / 2);
            palkkiHP.Y = Screen.Top - y;
            palkkiHP.BarColor = Color.Red;
            palkkiHP.BorderColor = Color.Black;
            palkkiHP.Color = Color.Gray;
            palkkiHP.BindTo(pelaajaHP);
            Add(palkkiHP);
        }

        /// <summary>
        /// Aliohjelma, joka luo MP-palkin näytölle.
        /// </summary>
        /// <param name="x">Palkin X-koordinaatti</param>
        /// <param name="y">Palkin Y-koordinaatti</param>
        /// <param name="leveys">MP-Palkin leveys, tämä kasvaa kun MAX MP arvo muuttuu isommaksi levelien myötä.</param>
        /// <param name="korkeus">MP-palkin korkeus, koska vaihtelee tilanne ikkunan ja pelinäkymän välillä</param>
        private void LuoPalkkiMP(double x = 115, double y = 30, double leveys = 1, double korkeus = 8)
        {
            ProgressBar palkkiMP = new ProgressBar(pelaajaMP.MaxValue * leveys, korkeus); //MP palkin määrittelyä
            palkkiMP.X = Screen.Left + x + (pelaajaMP.MaxValue * leveys / 2);
            palkkiMP.Y = Screen.Top - y;
            palkkiMP.BarColor = Color.Blue;
            palkkiMP.BorderColor = Color.Black;
            palkkiMP.Color = Color.Gray;
            palkkiMP.BindTo(pelaajaMP);
            Add(palkkiMP);
        }

        /// <summary>
        /// Aliohjelma, joka luo Stamina-palkin näytölle.
        /// </summary>
        /// <param name="x">Palkin X-koordinaatti</param>
        /// <param name="y">Palkin Y-koordinaatti</param>
        /// <param name="leveys">Stamina-Palkin leveys, tämä kasvaa kun MAX Stamina arvo muuttuu isommaksi levelien myötä.</param>
        /// <param name="korkeus">Stamina-palkin korkeus, koska vaihtelee tilanne ikkunan ja pelinäkymän välillä</param>
        private void LuoPalkkiStamina(double x = 115, double y = 22, double leveys = 1.0, double korkeus = 8)
        {
            ProgressBar palkkiStamina = new ProgressBar(pelaajaStamina.MaxValue * leveys, korkeus); //Stamina palkin määrittelyä
            palkkiStamina.X = Screen.Left + x + (pelaajaStamina.MaxValue * leveys / 2);
            palkkiStamina.Y = Screen.Top - y;
            palkkiStamina.BarColor = Color.BrightGreen;
            palkkiStamina.BorderColor = Color.Black;
            palkkiStamina.Color = Color.Gray;
            palkkiStamina.BindTo(pelaajaStamina);
            Add(palkkiStamina);
        }

        /// <summary>
        /// Aliohjelma, joka luo pelaajan jättimäisen profiilikuvan
        /// </summary>
        private void LuoWidgetti()
        {
            Widget kuvaPelaaja = new Widget(100.0, 100.0, Shape.Rectangle); //Profiilikuvawidgetin määrittelyä
            kuvaPelaaja.X = Screen.LeftSafe + 50;
            kuvaPelaaja.Y = Screen.TopSafe - 70;
            kuvaPelaaja.Color = Color.Gray;
            kuvaPelaaja.BorderColor = Color.Black;
            kuvaPelaaja.Image = LoadImage("Profiili");
            Add(kuvaPelaaja);
        }

        /// <summary>
        /// Aliohjelma joka luo edistymispalkin kenttään. Palkki kun on täynnä, niin pelaaja voi mennä maaliin
        /// </summary>
        private void LuoEdistymispalkki()
        {
            edistyminen = new DoubleMeter(0); //Palkki alkaa nollasta
            edistyminen.MaxValue = 10;
            edistyminen.UpperLimit += MaaliAuki; //Kun pelaaja saavuttaa maksimin, niin "maali on auki" eli pelaaja voi edetä bossiin ja maaliin

            ProgressBar palkkiEdistyminen = new ProgressBar(256, 8); //Palkin määrittelyä
            palkkiEdistyminen.X = Screen.Left + 137;
            palkkiEdistyminen.Y = Screen.Top - 125;
            palkkiEdistyminen.BarColor = Color.Yellow;
            palkkiEdistyminen.BorderColor = Color.Black;
            palkkiEdistyminen.Color = Color.Transparent;
            palkkiEdistyminen.BindTo(edistyminen);
            Add(palkkiEdistyminen);
        }

        /// <summary>
        /// Aliohjelma, joka luo tekstikentän oikeaan ylälaitaan. Tämä toimii tilannekatsauksena ja kertoo mitä pelaajalle tapahtuu pelissä
        /// </summary>
        /// <param name="x">X-koordinaattiin</param>
        /// <param name="teksti">Teksti joka näytetään oikeassa yläkulmassa</param>
        private void TilanneKatsaus(double x, string teksti)
        {
            info = new Label(x, 20); //Tekstikentän määrittelyä
            info.Text = teksti;
            info.X = Screen.Right - 10 - (x / 2);
            info.Y = Screen.Top - 20;
            info.BorderColor = Color.Transparent;
            info.Color = Color.Transparent;
            Add(info);
            Timer tekstikentta = new Timer(); //Tekstikenttä näkyy vain tietyn aikaa ylhäällä, koska miksi säilyttää vanha tieto, tosin olishan se mediaseksikästä nähdä HP + 1 koko ajan
            Timer.SingleShot(3.0, delegate { info.Destroy(); });
            tekstikentta.Start();
        }

        /// <summary>
        /// Aliohjelma joka arpoo minkä stat boostin pelaaja saa levelin myötä.
        /// </summary>
        private void StatValinta()
        {
            Random rand = new Random(); //random generaattori, joo ei ole kovin hyvä
            double viive = 3.1;
            TilanneKatsaus(200, "Tasosi nousi!");
            while (true) //Ikuinen silmukka kunnes jotain tapahtuu
            {
                int satunnainen = rand.Next(0, levelMAX - 1); //HP boosteja 10, Stamina boosteja 6, MP boosteja 3 = 19 boostia pelissä eli levelit 2-20 antaa boostin.
                if (satunnainen < 10) //Jos generoidaan alle 10 eli 0-9. Eli kyllä tämä on satunnaisarvonta jossa on melkein 50% mahdollisuus että HP nousee kunnes Max saavutettu
                {
                    if (pelaajaHP.MaxValue < maxHP) //Tarkistus että pelaajan max HP on alle pelin katon, jos ei ole alle niin generoidaan uusi powerup
                    {
                        pelaajaHP.MaxValue += 1; //Pelaajan max hp nousee pysyvästi
                        Timer.SingleShot(viive, delegate { TilanneKatsaus(200, "Max HP + 1"); }); //Printtaa vähän myöhässä tämä, että ei mene päälle toisen kanssa
                        LuoPalkkiHP(); //Generoi palkin uudestaan, jotta uusi isompi palkki saadaan näkyviin.
                        break;
                    }
                }
                else if (satunnainen < 16) //Arvot 10-15
                {
                    if (pelaajaStamina.MaxValue < maxStamina) //Onhan stamina alle pelin katon
                    {
                        pelaajaStamina.MaxValue += 10; //Nousee pysyvästi
                        Timer.SingleShot(viive, delegate { TilanneKatsaus(200, "Max Stamina + 10"); }); //Lagaa jäljessä tarkoituksella
                        LuoPalkkiStamina(); //Uusi palkki
                        break;
                    }
                }
                else if (satunnainen < levelMAX - 2) //Eli arvot 16-18
                {
                    if (pelaajaMP.MaxValue < maxMP) //Sanity check taas
                    {
                        pelaajaMP.MaxValue += 2; //Eiköhän tää kuvio ole jo ylemmästäkin selvää?
                        Timer.SingleShot(viive, delegate { TilanneKatsaus(200, "Max MP + 2"); });
                        LuoPalkkiMP();
                        break;
                    }
                }
                else if (pelaajaHP.MaxValue == maxHP && pelaajaStamina.MaxValue == maxStamina && pelaajaMP.MaxValue == maxMP) //Jos homma menee pieleen
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Luo expa palkin näkyviin
        /// </summary>
        /// <param name="x">Expa palkin x-koordinaatti</param>
        /// <param name="y">Y-koordinaatti</param>
        /// <param name="leveys">Expa palkin leveys</param>
        /// <param name="korkeus">Palkin korkeus</param>
        private void LuoExpapalkki(double x = 137, double y = 5, double leveys = 255, double korkeus = 6)
        {
            if (leveli < levelMAX) //Onko pelaajan mahdollista vielä levelittää? Jos level 20 niin ei luoda palkkia eli pelaaja ei voi levelittää
            {
                ProgressBar palkkiExpa = new ProgressBar(leveys, korkeus); //Määritetään palkki
                palkkiExpa.X = Screen.Left + x;
                palkkiExpa.Y = Screen.Top - y;
                palkkiExpa.BarColor = Color.Cyan;
                palkkiExpa.BorderColor = Color.Black;
                palkkiExpa.Color = Color.Gray;
                palkkiExpa.BindTo(expa);
                Add(palkkiExpa);
            }
        }

        /// <summary>
        /// Aliohjelma joka luo expan vaatimuksen seuraavaan leveliin
        /// </summary>
        /// <param name="level">Pelaajan leveli</param>
        /// <returns>Expamäärän seuraavaan leveliin eli näköjään leveli potenssiin 3</returns>
        ///<example>
        ///<pre name = "test" >
        ///ExpaVaatimus(2) === 8;
        ///ExpaVaatimus(3) === 27;
        ///ExpaVaatimus(11) === 1331;
        ///ExpaVaatimus(20) === 8000;
        ///</pre>
        ///</example>
        private double ExpaVaatimus(double level)
        {
            return Math.Pow(level, 3); //Palauttaa levelin potenssiin 3
        }

        /// <summary>
        /// Aliohjelma joka laskee pelaajan tekemän damagen
        /// </summary>
        /// <param name="leveli">pelaajan leveli</param>
        /// <returns>Pelaajan tekemän damagen, käytetään bosseissa</returns>
        ///<example>
        ///<pre name = "test" >
        ///PelaajaDamage(15) === 7.75;
        ///PelaajaDamage(3) === 2.35;
        ///PelaajaDamage(20) === 10;
        ///</pre>
        ///</example>
        private double PelaajaDamage(double leveli) //Laskee pelaajan damagen, käytännössä jokainen leveli nostaa 0.45 pykälää defaultista 1.45 joka level, maksimi 10 kun level 20 
        {
            return 1 + leveli;
        }

        /// <summary>
        /// Aliohjelma joka palauttaa tähtiarvion pelaajan kenttä suorituksesta
        /// </summary>
        /// <param name="osumat">Kuinka monta osumaa pelaaja on ottanut</param>
        /// <returns>Palauttaa pisteytyksen</returns>
        private double Rating(int osumat)
        {
            double tavoite = 300 + (kentta * 180); //Luodaan kenttään oma tavoite-aika. 300, 480, 660, 840 sekuntia kentille 0-3
            tavoite = tavoite / 2; //Jaetaan tavoiteaika puoliksi -> 150, 240, 330, 420s
            tavoite -= 60 * kentta; //Miinustetaan vielä 1min per kentän numero ajasta eli, 150, 180, 270, 360s ovat tavoitteita, jotka pitää olla kentän lopussa jäljellä.

            double apu = aikaAjastin.Value; //Ottaa kentän ajan vertailukohdaksi
            double pisteet = apu - (osumat * 2); //Pelaajan saamat pisteet, eli jäljellä oleva-aika, josta vähennetään vielä osumien viemät pisteet
            pisteet = pisteet / tavoite * 5; //Tehdään prosentuaalinen arvio ja kerrotaan 5 "tähdellä". Eli 0 osumaa ja tavoitteen yli jääminen -> 5* automaattisesti.

            pisteet = Math.Round(pisteet); //Pyöristetään kokonaiseksi tähdeksi
            if (pisteet > 5) //Jos yli tavoiteajan ja ei osumia esimerkiksi niin pyöristetään 5*
            {
                pisteet = 5;
            }
            else if (pisteet < 0) //Jos menee tosi huonosti, eli hirveesti osumia ja melkein 0s jäljellä
            {
                pisteet = 0;
            }

            return pisteet; //Palautetaan tähtiarvio, 0-5
        }

        /// <summary>
        /// Aliohjelma, joka katsoo saiko pelaaja paremman arvosanan kuin viime kerralla, jos arvosana on suurempi niin päivittää isommaksi
        /// </summary>
        /// <param name="arvio">Kentän suorittamisesta saatu arvio, tätä verrataan vanhaan</param>
        private void Arvostelu(double arvio)
        {
            if (arvio > arvosanat[kentta]) //Onko uusi pisteytys parempi kuin edellinen? Jos kyllä niin muutetaan arvio.
            {
                arvosanat[kentta] = arvio;
            }
        }
    }
