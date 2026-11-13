namespace ReflavorRP.Tests.Accents;

public class PortugueseFrontalLispTests
{
    private static readonly Accent Lisp = Template("Frontal lisp (Portuguese)");

    [Theory]
    // s, ss, ç, z and soft c, anywhere in the word
    [InlineData("sim", "fim")]
    [InlineData("passo", "pafo")]
    [InlineData("casa", "cafa")]
    [InlineData("coração", "corafão")]
    [InlineData("cedo", "fedo")]
    [InlineData("cidade", "fidade")]
    [InlineData("você", "vofê")]
    [InlineData("fazer", "fafer")]
    [InlineData("zero", "fero")]
    [InlineData("mais", "maif")]
    [InlineData("vamos", "vamof")]
    [InlineData("festa", "fefta")]
    // runs of s sounds are one f
    [InlineData("nascer", "nafer")]
    [InlineData("desça", "defa")]
    [InlineData("piscina", "pifina")]
    [InlineData("excelente", "efelente")]
    [InlineData("exsudação", "efudafão")]
    // x said s or z is an f, x said ks keeps the k
    [InlineData("exame", "efame")]
    [InlineData("texto", "tefto")]
    [InlineData("próximo", "prófimo")]
    [InlineData("táxi", "tákfi")]
    [InlineData("sexo", "fekfo")]
    [InlineData("tórax", "tórakf")]
    // capitals are kept
    [InlineData("Sim", "Fim")]
    [InlineData("SIM", "FIM")]
    [InlineData("TAXI", "TAKFI")]
    // hard c, ch and English sh are not s sounds
    [InlineData("cachorro", "cachorro")]
    [InlineData("chuva", "chuva")]
    [InlineData("show", "show")]
    public void LispsPortugueseSSounds(string input, string expected)
    {
        Assert.Equal(expected, Lisp.Apply(input));
    }

    // Word lists by the sound of x, from Norma Culta, Toda Matéria, Dicio and Atividades para
    // Educadores, plus words the rules single out. Only the x is checked; the cases above cover the rest.

    public static TheoryData<string> XSaidSh => new(
        "abacaxi", "Alexandre", "ameixa", "baixo", "bexiga", "bruxa", "buxo", "caixa", "compaixão", "coxa",
        "coxinha", "coxo", "deixar", "eixo", "engraxar", "enxada", "enxame", "enxaqueca", "enxergar",
        "enxerido", "enxerto", "enxofre", "enxoval", "enxugar", "enxurrada", "enxuto", "faixa", "faxina",
        "feixe", "frouxo", "lagartixa", "lixeira", "lixo", "luxo", "maxixe", "mexa", "mexer", "mexerica",
        "mexicano", "mexido", "mexilhão", "orixá", "oxalá", "paixão", "peixaria", "peixe", "praxe", "puxar",
        "queixo", "relaxar", "roxo", "taxa", "texugo", "trouxa", "vexame", "xá", "xadrez", "xale", "xampu",
        "xará", "xarope", "xavante", "xeque", "xerife", "xícara", "xingar", "xodó");

    public static TheoryData<string> XSaidKs => new(
        "anexo", "anorexia", "asfixia", "axila", "axioma", "boxe", "circunflexo", "complexo", "conexão",
        "convexo", "crucifixo", "dióxido", "dislexia", "fênix", "fixo", "flexão", "flexível", "fluxo",
        "intoxicação", "intoxicar", "látex", "léxico", "maxilar", "monóxido", "nexo", "ortodoxo", "óxido",
        "oxigênio", "paradoxo", "paroxítona", "práxis", "prefixo", "reflexão", "reflexo", "Rex", "saxofone",
        "sexagésimo", "sexual", "táxi", "taxímetro", "taxonomia", "tórax", "tóxico", "toxina");

    public static TheoryData<string> XSaidSOrZ => new(
        // s before a consonant
        "contexto", "êxtase", "excludente", "excluído", "excluir", "exclamação", "exclamar", "exclusividade",
        "exclusivo", "excomungar", "excremento", "excruciante", "excursão", "exfoliação", "expansão",
        "expectativa", "experiência", "experiente", "experimentar", "expirar", "explicação", "explicar",
        "explícito", "explodir", "explorar", "explosão", "explosivo", "exposição", "expressar", "expresso",
        "exprimir", "expulsar", "extensão", "extensível", "extenso", "exterior", "exteriorizar",
        "exterminador", "extermínio", "externo", "extinção", "extinguir", "extinto", "extintor", "extra",
        "extracurricular", "extraescolar", "extrair", "extremidade", "extremo", "extrínseco",
        "extrovertido", "exventrar", "sexta", "sexto", "têxtil", "texto", "textual",
        // ss between vowels
        "aproximação", "aproximar", "auxiliador", "auxiliar", "auxílio", "máximo", "maximizar",
        "proximidade", "próximo", "sintaxe", "trouxe",
        // z between vowels
        "coexistir", "exagerado", "exagero", "exalar", "exaltado", "exaltar", "exame", "examinar",
        "exasperado", "exato", "exaustivo", "exausto", "execrável", "executar", "executivo", "exemplar",
        "exemplificar", "exemplo", "exequível", "exercício", "exército", "exibir", "exigência", "exigir",
        "exilado", "exílio", "existência", "existir", "êxito", "êxodo", "exorcizar", "exótico",
        "exuberante", "inexistente",
        // xc and xs are one s sound
        "excedente", "exceder", "excelência", "excelente", "excelentíssimo", "excentricidade", "excêntrico",
        "exceção", "excepcional", "excepcionalidade", "excerto", "excessivo", "excesso", "exceto",
        "excitação", "excitante", "excitar", "exsudação");

    [Theory]
    [MemberData(nameof(XSaidSh))]
    public void XSaidShStays(string word)
    {
        Assert.Contains("x", Lisp.Apply(word), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(XSaidKs))]
    public void XSaidKsKeepsTheK(string word)
    {
        var result = Lisp.Apply(word);
        Assert.DoesNotContain("x", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kf", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(XSaidSOrZ))]
    public void XSaidSOrZIsLisped(string word)
    {
        var result = Lisp.Apply(word);
        Assert.DoesNotContain("x", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kf", result, StringComparison.OrdinalIgnoreCase);
    }
}
