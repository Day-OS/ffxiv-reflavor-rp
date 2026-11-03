namespace ReflavorRP.Tests.Accents;

public class FrontalLispTests
{
    private static readonly Accent Lisp = Template("Frontal lisp");

    [Theory]
    // s, z and soft c, anywhere in the word
    [InlineData("sorry", "thorry")]
    [InlineData("yes", "yeth")]
    [InlineData("first", "firtht")]
    [InlineData("answer", "anthwer")]
    [InlineData("is", "ith")]
    [InlineData("rise", "rithe")]
    [InlineData("zero", "thero")]
    [InlineData("lazy", "lathy")]
    [InlineData("city", "thity")]
    [InlineData("cycle", "thycle")]
    [InlineData("face", "fathe")]
    [InlineData("mercy", "merthy")]
    [InlineData("decide", "dethide")]
    [InlineData("decision", "dethision")]
    [InlineData("please", "pleathe")]
    [InlineData("house", "houthe")]
    [InlineData("this", "thith")]
    [InlineData("sister", "thithter")]
    [InlineData("sweet", "thweet")]
    [InlineData("street", "thtreet")]
    [InlineData("strength", "thtrength")]
    [InlineData("whisper", "whithper")]
    [InlineData("listen", "lithten")]
    [InlineData("castle", "cathtle")]
    [InlineData("Christmas", "Chrithtmath")]
    [InlineData("sword", "thword")]
    [InlineData("question", "quethtion")]
    [InlineData("isn't", "ithn't")]
    [InlineData("receive", "retheive")]
    [InlineData("ceiling", "theiling")]
    [InlineData("bicycle", "bithycle")]
    [InlineData("policy", "polithy")]
    [InlineData("voice", "voithe")]
    [InlineData("zone", "thone")]
    [InlineData("quiz", "quith")]
    [InlineData("citizen", "thitithen")]
    [InlineData("seize", "theithe")]
    [InlineData("waltz", "waltth")]
    // s before a hard c
    [InlineData("scar", "thcar")]
    [InlineData("describe", "dethcribe")]
    [InlineData("mascot", "mathcot")]
    // hard c is left alone
    [InlineData("cat", "cat")]
    [InlineData("occur", "occur")]
    public void LispSounds(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Theory]
    [InlineData("miss", "mith")]
    [InlineData("glass", "glath")]
    [InlineData("sassy", "thathy")]
    [InlineData("possess", "potheth")]
    [InlineData("pizza", "pitha")]
    [InlineData("buzz", "buth")]
    [InlineData("jazz", "jath")]
    [InlineData("fizzy", "fithy")]
    [InlineData("science", "thienthe")]
    [InlineData("scene", "thene")]
    [InlineData("scissors", "thithorth")]
    [InlineData("fascinate", "fathinate")]
    [InlineData("descend", "dethend")]
    [InlineData("ascend", "athend")]
    [InlineData("princess", "printheth")]
    [InlineData("hostess", "hothteth")]
    [InlineData("kisses", "kitheth")]
    [InlineData("blizzard", "blithard")]
    [InlineData("squeeze", "thqueethe")]
    [InlineData("blitz", "blitth")]
    [InlineData("Mississippi", "Mithithippi")]
    [InlineData("sorcerer", "thortherer")]
    [InlineData("versus", "verthuth")]
    [InlineData("decisive", "dethithive")]
    [InlineData("precise", "prethithe")]
    [InlineData("scythe", "thythe")]
    [InlineData("obsess", "obtheth")]
    [InlineData("assassin", "athathin")]
    [InlineData("message", "methage")]
    [InlineData("sensation", "thenthation")]
    [InlineData("silence", "thilenthe")]
    [InlineData("crescent", "crethent")]
    public void RunOfSibilantsIsOneTh(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Theory]
    [InlineData("box", "bokth")]
    [InlineData("fox", "fokth")]
    [InlineData("taxi", "takthi")]
    [InlineData("next", "nektht")]
    [InlineData("exit", "ekthit")]
    [InlineData("excellent", "ekthellent")]
    [InlineData("exceed", "ektheed")]
    [InlineData("accept", "akthept")]
    [InlineData("accent", "akthent")]
    [InlineData("vaccine", "vakthine")]
    [InlineData("success", "thuktheth")]
    [InlineData("access", "aktheth")]
    [InlineData("accident", "akthident")]
    [InlineData("succinct", "thukthinct")]
    [InlineData("boxes", "boktheth")]
    [InlineData("expression", "ekthpression")]
    [InlineData("anxiety", "ankthiety")]
    // silent h after x
    [InlineData("exhausted", "ekthauthted")]
    [InlineData("complexion", "complekshion")]
    // x at the start sounds like z
    [InlineData("xylophone", "thylophone")]
    [InlineData("Xena", "Thena")]
    // x as ksh
    [InlineData("anxious", "ankshiouth")]
    [InlineData("obnoxious", "obnokshiouth")]
    [InlineData("luxury", "lukshury")]
    public void XAndDoubleC(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Theory]
    // sch at the start is sk
    [InlineData("school", "thkool")]
    [InlineData("scheme", "thkeme")]
    [InlineData("scholar", "thkolar")]
    [InlineData("schedule", "thkedule")]
    // anywhere else it's s + ch
    [InlineData("mischief", "mithchief")]
    [InlineData("discharge", "dithcharge")]
    // silent p
    [InlineData("psychic", "thychic")]
    [InlineData("pseudo", "theudo")]
    [InlineData("psalm", "thalm")]
    // p is not silent mid word
    [InlineData("lapse", "lapthe")]
    [InlineData("upset", "upthet")]
    public void SchAndPs(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Theory]
    [InlineData("shoe", "shoe")]
    [InlineData("fish", "fish")]
    // -sion, -ssion, -sia
    [InlineData("mission", "mission")]
    [InlineData("passion", "passion")]
    [InlineData("vision", "vision")]
    [InlineData("fusion", "fusion")]
    [InlineData("tension", "tension")]
    [InlineData("Russia", "Russia")]
    [InlineData("Asian", "Asian")]
    [InlineData("Persian", "Persian")]
    [InlineData("version", "version")]
    [InlineData("television", "television")]
    // but sci-/si- at the start is a real s
    [InlineData("Scion", "Thion")]
    // -sual
    [InlineData("usual", "usual")]
    [InlineData("unusual", "unusual")]
    [InlineData("casual", "casual")]
    [InlineData("visual", "visual")]
    // -cia, -cious, -cient, -cean
    [InlineData("special", "thpecial")]
    [InlineData("official", "official")]
    [InlineData("delicious", "deliciouth")]
    [InlineData("precious", "preciouth")]
    [InlineData("conscious", "consciouth")]
    [InlineData("suspicious", "thuthpiciouth")]
    [InlineData("conscientious", "conscientiouth")]
    [InlineData("sufficient", "thufficient")]
    [InlineData("permission", "permission")]
    [InlineData("ancient", "ancient")]
    [InlineData("efficient", "efficient")]
    [InlineData("ocean", "ocean")]
    // -sure, -zure, -surance
    [InlineData("sure", "sure")]
    [InlineData("measure", "measure")]
    [InlineData("pleasure", "pleasure")]
    [InlineData("treasure", "treasure")]
    [InlineData("pressure", "pressure")]
    [InlineData("closure", "closure")]
    [InlineData("azure", "azure")]
    [InlineData("seizure", "theizure")]
    [InlineData("insurance", "insuranthe")]
    [InlineData("assurance", "assuranthe")]
    // sugar, issue
    [InlineData("sugar", "sugar")]
    [InlineData("issue", "issue")]
    [InlineData("tissue", "tissue")]
    // but sue is a real s
    [InlineData("sue", "thue")]
    [InlineData("pursue", "purthue")]
    public void ShAndZhSoundsAreNotLisped(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Theory]
    [InlineData("island", "island")]
    [InlineData("islands", "islandth")]
    [InlineData("isle", "isle")]
    [InlineData("aisle", "aisle")]
    // not silent
    [InlineData("this land", "thith land")]
    [InlineData("Islam", "Ithlam")]
    public void SilentS(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Theory]
    [InlineData("S A D", "'Eth' A D")]
    [InlineData("S-A-D", "'Eth'-A-D")]
    [InlineData("s", "'eth'")]
    [InlineData("Z", "'Thee'")]
    [InlineData("X marks the spot", "'Ekth' markth the thpot")]
    [InlineData("plan B or plan C", "plan B or plan 'Thee'")]
    // letters that have no s sound in their name stay
    [InlineData("A B D", "A B D")]
    // but 's is a word ending, not a letter
    [InlineData("Sam's", "Tham'th")]
    [InlineData("it's", "it'th")]
    public void SpelledLettersAreSaidByName(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    [Theory]
    [InlineData("Eorzea", "Eorthea")]
    [InlineData("Limsa Lominsa", "Limtha Lomintha")]
    [InlineData("Alisaie", "Alithaie")]
    [InlineData("Estinien", "Ethtinien")]
    [InlineData("Zenos", "Thenoth")]
    [InlineData("Zodiark", "Thodiark")]
    [InlineData("Crystal", "Crythtal")]
    [InlineData("Scions", "Thionth")]
    [InlineData("Ishgard", "Ishgard")]
    [InlineData("Sharlayan", "Sharlayan")]
    [InlineData("Y'shtola", "Y'shtola")]
    // places
    [InlineData("Radz-at-Han", "Radth-at-Han")]
    [InlineData("Coerthas", "Coerthath")]
    [InlineData("Azim Steppe", "Athim Thteppe")]
    [InlineData("Ilsabard", "Ilthabard")]
    [InlineData("Sastasha", "Thathtasha")]
    [InlineData("Dzemael Darkhold", "Dthemael Darkhold")]
    [InlineData("Sohm Al", "Thohm Al")]
    [InlineData("Xelphatol", "Thelphatol")]
    [InlineData("Bozja", "Bothja")]
    [InlineData("Gold Saucer", "Gold Thauther")]
    [InlineData("Hingashi", "Hingashi")]
    [InlineData("Ishgardian", "Ishgardian")]
    [InlineData("Eorzean", "Eorthean")]
    // people
    [InlineData("Ysayle", "Ythayle")]
    [InlineData("Hraesvelgr", "Hraethvelgr")]
    [InlineData("Emet-Selch", "Emet-Thelch")]
    [InlineData("Hythlodaeus", "Hythlodaeuth")]
    [InlineData("Azem", "Athem")]
    [InlineData("Elidibus", "Elidibuth")]
    [InlineData("Asahi", "Athahi")]
    [InlineData("Yotsuyu", "Yotthuyu")]
    [InlineData("Gaius", "Gaiuth")]
    [InlineData("Cid", "Thid")]
    [InlineData("Lyse", "Lythe")]
    [InlineData("Kan-E-Senna", "Kan-E-Thenna")]
    [InlineData("Crystal Exarch", "Crythtal Ektharch")]
    [InlineData("Louisoix", "Louithoix")]
    [InlineData("Gosetsu", "Gothetthu")]
    [InlineData("Golbez", "Golbeth")]
    [InlineData("Sphene", "Thphene")]
    [InlineData("Zoraal Ja", "Thoraal Ja")]
    [InlineData("Minfilia", "Minfilia")]
    // primals
    [InlineData("Bismarck", "Bithmarck")]
    [InlineData("Sephirot", "Thephirot")]
    [InlineData("Susano", "Thuthano")]
    [InlineData("Suzaku", "Thuthaku")]
    [InlineData("Tsukuyomi", "Tthukuyomi")]
    [InlineData("Innocence", "Innothenthe")]
    [InlineData("Endsinger", "Endthinger")]
    [InlineData("Hesperos", "Hethperoth")]
    [InlineData("Zeromus", "Theromuth")]
    [InlineData("Lakshmi", "Lakshmi")]
    [InlineData("Shiva", "Shiva")]
    // jobs and races
    [InlineData("Sage", "Thage")]
    [InlineData("Scholar", "Thkolar")]
    [InlineData("Astrologian", "Athtrologian")]
    [InlineData("Summoner", "Thummoner")]
    [InlineData("Dancer", "Danther")]
    [InlineData("Samurai", "Thamurai")]
    [InlineData("Machinist", "Machinitht")]
    [InlineData("Pictomancer", "Pictomanther")]
    [InlineData("Elezen", "Elethen")]
    [InlineData("Sylph", "Thylph")]
    [InlineData("Ixal", "Ikthal")]
    [InlineData("Namazu", "Namathu")]
    [InlineData("Arkasodara", "Arkathodara")]
    public void FfxivNames(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }
}
