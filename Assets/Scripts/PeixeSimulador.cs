using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI; 
using System.Globalization; 

[System.Serializable]
public class DadosMinuto
{
    public string nomeMinuto; 
    [Range(0,1)] public float probFundo1; 
    [Range(0,1)] public float probFundo2;
    [Range(0,1)] public float probTopo3;
    [Range(0,1)] public float probTopo4;
    public float velocidadeMedia;
    public float velocidadeMaxima;
    [Range(0,1)] public float chanceEstarMovendo = 0.5f; 
}

public class PeixeSimulador : MonoBehaviour
{
    [Header("--- VERSÃO FINAL (SANDBOX INTELIGENTE) ---")]
    public bool iniciarAutomaticamente = false; 

    [Header("UI (Interface Geral)")]
    public TextMeshProUGUI textoModoAtual; 
    public Button btnNormal; 
    public Button btnAlcool; 
    public Button btnAlcoolAlto; 
    public Button btnLivre; 
    public TextMeshProUGUI textoAviso; 

    [Header("Painel de Variáveis (Alternância)")]
    [Tooltip("O texto simples de leitura para os modos programados")]
    public TextMeshProUGUI textoVariaveisLeitura; 
    
    [Tooltip("O objeto pai que guarda todas as caixas de input do Modo Livre")]
    public GameObject grupoInputsEdicao; 

    [Header("Inputs do Modo Livre")]
    public TextMeshProUGUI textoMinutoAtual; 
    public TMP_InputField inputProb1;
    public TMP_InputField inputProb2;
    public TMP_InputField inputProb3;
    public TMP_InputField inputProb4;
    public TMP_InputField inputVelMedia;
    public TMP_InputField inputVelMax;
    public TMP_InputField inputChance;

    [Header("Sistemas Externos")]
    public RelogioSimulacao relogioSimulacao; 

    [Header("Sistema de Comparação (Fantasma)")]
    public GameObject peixeFantasma; 
    private List<DadosMinuto> timelineFantasma;
    private Coroutine rotinaViradaFantasma;

    [Header("Configuração de Espaço")]
    public Transform linhaCentral; 
    public float alturaAguaTotal = 8f; 
    public float limiteMinX = -8f; 
    public float limiteMaxX = 8f;
    public float tempoDeLatencia = 215f; 

    [Header("Escala da Simulação")]
    public float fatorDeEscala = 0.08f;

    [Header("Dados (Visualização)")]
    public List<DadosMinuto> timelineComportamento;

    // Estado Interno
    private float tempoSimulacao = 0f;
    private bool simulando = false;
    private int indiceMinutoAtual = 0;
    private float[] limitesZonasY; 
    private float escalaXOriginal;
    private Coroutine rotinaVirada;
    private bool modoLivreAtivo = false; 

    void Awake()
    {
        escalaXOriginal = Mathf.Abs(transform.localScale.x);
        timelineComportamento = new List<DadosMinuto>();
        timelineFantasma = new List<DadosMinuto>();
        
        ConfigurarZonas();
        CarregarDadosNormais(); 
        CarregarDadosFantasma(); 
    }

    void Start()
    {
        if (Time.timeScale == 0) Time.timeScale = 1f;

        Animator anim = GetComponent<Animator>();
        if (anim != null && anim.enabled) anim.enabled = false;

        if (linhaCentral == null)
            Debug.LogError("ERRO: Arraste o objeto ReferenciaCentro para o campo Linha Central!");

        // Configurações iniciais
        if (textoModoAtual != null) textoModoAtual.text = "Modo: Controle Negativo (Normal)";
        if (btnNormal != null) btnNormal.interactable = false;
        if (btnAlcool != null) btnAlcool.interactable = true;
        if (btnAlcoolAlto != null) btnAlcoolAlto.interactable = true;
        if (btnLivre != null) btnLivre.interactable = true;
        if (textoAviso != null) textoAviso.text = "";
        if (peixeFantasma != null) peixeFantasma.SetActive(false);

        // Como o jogo começa no modo Normal, ativamos a leitura e desligamos a edição
        if (textoVariaveisLeitura != null) textoVariaveisLeitura.gameObject.SetActive(true);
        if (grupoInputsEdicao != null) grupoInputsEdicao.SetActive(false);

        ConfigurarListenersDeEdicao();

        if (iniciarAutomaticamente) IniciarSimulacao();
    }

    // --- MÉTODOS PARA OS BOTÕES DA UI ---

    public void IniciarSimulacaoNormal()
    {
        PararSimulacao();
        CarregarDadosNormais();
        modoLivreAtivo = false;
        
        if (textoModoAtual != null) textoModoAtual.text = "Modo: Controle Negativo (Normal)";
        if (btnNormal != null) btnNormal.interactable = false;
        if (btnAlcool != null) btnAlcool.interactable = true;
        if (btnAlcoolAlto != null) btnAlcoolAlto.interactable = true;
        if (btnLivre != null) btnLivre.interactable = true;
        if (peixeFantasma != null) peixeFantasma.SetActive(false);

        if (textoVariaveisLeitura != null) textoVariaveisLeitura.gameObject.SetActive(true);
        if (grupoInputsEdicao != null) grupoInputsEdicao.SetActive(false);

        ExibirAvisoTemporario("Simulação Reiniciada!"); 
        IniciarSimulacao();
    }

    public void IniciarSimulacaoAlcool()
    {
        PararSimulacao();
        CarregarDadosAlcool();
        modoLivreAtivo = false;
        
        if (textoModoAtual != null) textoModoAtual.text = "Modo: Álcool 0,5%";
        if (btnNormal != null) btnNormal.interactable = true;
        if (btnAlcool != null) btnAlcool.interactable = false;
        if (btnAlcoolAlto != null) btnAlcoolAlto.interactable = true;
        if (btnLivre != null) btnLivre.interactable = true;
        if (peixeFantasma != null) peixeFantasma.SetActive(true);

        if (textoVariaveisLeitura != null) textoVariaveisLeitura.gameObject.SetActive(true);
        if (grupoInputsEdicao != null) grupoInputsEdicao.SetActive(false);

        ExibirAvisoTemporario("Simulação Reiniciada!"); 
        IniciarSimulacao();
    }

    public void IniciarSimulacaoAlcoolAlto()
    {
        PararSimulacao();
        CarregarDadosAlcoolAlto();
        modoLivreAtivo = false;
        
        if (textoModoAtual != null) textoModoAtual.text = "Modo: Álcool 2,0%";
        if (btnNormal != null) btnNormal.interactable = true;
        if (btnAlcool != null) btnAlcool.interactable = true;
        if (btnAlcoolAlto != null) btnAlcoolAlto.interactable = false;
        if (btnLivre != null) btnLivre.interactable = true;
        if (peixeFantasma != null) peixeFantasma.SetActive(true);

        if (textoVariaveisLeitura != null) textoVariaveisLeitura.gameObject.SetActive(true);
        if (grupoInputsEdicao != null) grupoInputsEdicao.SetActive(false);

        ExibirAvisoTemporario("Simulação Reiniciada!"); 
        IniciarSimulacao();
    }

    public void IniciarSimulacaoLivre()
    {
        PararSimulacao();
        CarregarDadosNormais(); 
        modoLivreAtivo = true;
        
        if (textoModoAtual != null) textoModoAtual.text = "Modo: Livre (Customizável)";
        if (btnNormal != null) btnNormal.interactable = true;
        if (btnAlcool != null) btnAlcool.interactable = true;
        if (btnAlcoolAlto != null) btnAlcoolAlto.interactable = true;
        if (btnLivre != null) btnLivre.interactable = false;
        if (peixeFantasma != null) peixeFantasma.SetActive(true); 

        if (textoVariaveisLeitura != null) textoVariaveisLeitura.gameObject.SetActive(false);
        if (grupoInputsEdicao != null) grupoInputsEdicao.SetActive(true);

        ExibirAvisoTemporario("Modo Livre Ativado!"); 
        IniciarSimulacao();
    }

    // --- NOVO MÉTODO: RESETAR MODO LIVRE ---
    public void ResetarVariaveisLivre()
    {
        if (!modoLivreAtivo) return;

        // Recarrega os dados padrão na memória
        CarregarDadosNormais();

        // Força a atualização imediata das caixas de texto com os dados zerados
        DadosMinuto dadosAtuais = (indiceMinutoAtual < timelineComportamento.Count) 
            ? timelineComportamento[indiceMinutoAtual] 
            : timelineComportamento[timelineComportamento.Count - 1];

        if (inputProb1 != null) inputProb1.text = (dadosAtuais.probFundo1 * 100).ToString("F0");
        if (inputProb2 != null) inputProb2.text = (dadosAtuais.probFundo2 * 100).ToString("F0");
        if (inputProb3 != null) inputProb3.text = (dadosAtuais.probTopo3 * 100).ToString("F0");
        if (inputProb4 != null) inputProb4.text = (dadosAtuais.probTopo4 * 100).ToString("F0");
        if (inputVelMedia != null) inputVelMedia.text = dadosAtuais.velocidadeMedia.ToString("F2");
        if (inputVelMax != null) inputVelMax.text = dadosAtuais.velocidadeMaxima.ToString("F2");
        if (inputChance != null) inputChance.text = (dadosAtuais.chanceEstarMovendo * 100).ToString("F0");

        ExibirAvisoTemporario("Valores Restaurados!");
        Debug.Log("Variáveis do Modo Livre foram resetadas para o Controle Negativo.");
    }

    // --- SISTEMA DE EDIÇÃO EM TEMPO REAL (SANDBOX) ---

    void ConfigurarListenersDeEdicao()
    {
        if(inputProb1 != null) inputProb1.onEndEdit.AddListener(delegate { ReceberEdicaoDoUsuario(); });
        if(inputProb2 != null) inputProb2.onEndEdit.AddListener(delegate { ReceberEdicaoDoUsuario(); });
        if(inputProb3 != null) inputProb3.onEndEdit.AddListener(delegate { ReceberEdicaoDoUsuario(); });
        if(inputProb4 != null) inputProb4.onEndEdit.AddListener(delegate { ReceberEdicaoDoUsuario(); });
        if(inputVelMedia != null) inputVelMedia.onEndEdit.AddListener(delegate { ReceberEdicaoDoUsuario(); });
        if(inputVelMax != null) inputVelMax.onEndEdit.AddListener(delegate { ReceberEdicaoDoUsuario(); });
        if(inputChance != null) inputChance.onEndEdit.AddListener(delegate { ReceberEdicaoDoUsuario(); });
    }

    public void ReceberEdicaoDoUsuario()
    {
        if (!modoLivreAtivo || timelineComportamento == null || timelineComportamento.Count == 0 || !simulando) return;

        DadosMinuto dadosAtuais = (indiceMinutoAtual < timelineComportamento.Count) 
            ? timelineComportamento[indiceMinutoAtual] 
            : timelineComportamento[timelineComportamento.Count - 1];

        dadosAtuais.probFundo1 = LerInput(inputProb1.text, dadosAtuais.probFundo1 * 100f) / 100f;
        dadosAtuais.probFundo2 = LerInput(inputProb2.text, dadosAtuais.probFundo2 * 100f) / 100f;
        dadosAtuais.probTopo3 = LerInput(inputProb3.text, dadosAtuais.probTopo3 * 100f) / 100f;
        dadosAtuais.probTopo4 = LerInput(inputProb4.text, dadosAtuais.probTopo4 * 100f) / 100f;
        dadosAtuais.velocidadeMedia = LerInput(inputVelMedia.text, dadosAtuais.velocidadeMedia);
        dadosAtuais.velocidadeMaxima = LerInput(inputVelMax.text, dadosAtuais.velocidadeMaxima);
        dadosAtuais.chanceEstarMovendo = LerInput(inputChance.text, dadosAtuais.chanceEstarMovendo * 100f) / 100f;
    }

    float LerInput(string texto, float valorPadrao)
    {
        if (string.IsNullOrEmpty(texto)) return valorPadrao;
        string txtLimpo = texto.Replace("%", "").Replace("cm/s", "").Trim().Replace(",", "."); 
        if (float.TryParse(txtLimpo, NumberStyles.Any, CultureInfo.InvariantCulture, out float resultado))
            return resultado;
        
        return valorPadrao; 
    }

    void AtualizarPainelVariaveis(DadosMinuto dados)
    {
        if (modoLivreAtivo)
        {
            if (textoMinutoAtual != null) textoMinutoAtual.text = "Minuto Atual: " + dados.nomeMinuto;
            AtualizarInputSeNaoFocado(inputProb1, (dados.probFundo1 * 100).ToString("F0"));
            AtualizarInputSeNaoFocado(inputProb2, (dados.probFundo2 * 100).ToString("F0"));
            AtualizarInputSeNaoFocado(inputProb3, (dados.probTopo3 * 100).ToString("F0"));
            AtualizarInputSeNaoFocado(inputProb4, (dados.probTopo4 * 100).ToString("F0"));
            AtualizarInputSeNaoFocado(inputVelMedia, dados.velocidadeMedia.ToString("F2"));
            AtualizarInputSeNaoFocado(inputVelMax, dados.velocidadeMaxima.ToString("F2"));
            AtualizarInputSeNaoFocado(inputChance, (dados.chanceEstarMovendo * 100).ToString("F0"));
        }
        else
        {
            if (textoVariaveisLeitura != null)
            {
                textoVariaveisLeitura.text = 
                    $"<b>Minuto Atual:</b> {dados.nomeMinuto}\n" +
                    $"<b>Prob. Fundo 1:</b> {(dados.probFundo1 * 100).ToString("F0")}%\n" +
                    $"<b>Prob. Fundo 2:</b> {(dados.probFundo2 * 100).ToString("F0")}%\n" +
                    $"<b>Prob. Topo 3:</b> {(dados.probTopo3 * 100).ToString("F0")}%\n" +
                    $"<b>Prob. Topo 4:</b> {(dados.probTopo4 * 100).ToString("F0")}%\n" +
                    $"<b>Veloc. Média:</b> {dados.velocidadeMedia} cm/s\n" +
                    $"<b>Veloc. Máx:</b> {dados.velocidadeMaxima} cm/s\n" +
                    $"<b>Chance de Mover:</b> {(dados.chanceEstarMovendo * 100).ToString("F0")}%";
            }
        }
    }

    void AtualizarInputSeNaoFocado(TMP_InputField input, string novoTexto)
    {
        if (input != null && !input.isFocused)
        {
            input.text = novoTexto;
        }
    }

    // --- SISTEMA DE AVISO TEMPORÁRIO ---

    private Coroutine rotinaAviso; 
    void ExibirAvisoTemporario(string mensagem)
    {
        if (textoAviso == null) return;
        if (rotinaAviso != null) StopCoroutine(rotinaAviso);
        rotinaAviso = StartCoroutine(RotinaAviso(mensagem));
    }

    IEnumerator RotinaAviso(string mensagem)
    {
        textoAviso.text = mensagem;
        yield return new WaitForSeconds(2.5f); 
        textoAviso.text = ""; 
    }

    // --- CONTROLE PRINCIPAL DA SIMULAÇÃO ---

    public void IniciarSimulacao()
    {
        if (timelineComportamento == null || timelineComportamento.Count == 0)
             CarregarDadosNormais();

        if (!simulando)
        {
            simulando = true;
            tempoSimulacao = 0f;
            
            if (relogioSimulacao != null) relogioSimulacao.IniciarRelogio();
            
            if (linhaCentral != null)
            {
                transform.position = GerarDestinoNaZona(1);
                if (peixeFantasma != null && peixeFantasma.activeSelf) 
                    peixeFantasma.transform.position = GerarDestinoNaZona(1);
            }
            
            StartCoroutine(CicloDeVida());
            
            if (peixeFantasma != null && peixeFantasma.activeSelf)
                StartCoroutine(CicloDeVidaFantasma());
        }
    }

    public void PararSimulacao()
    {
        simulando = false;
        StopAllCoroutines(); 
        
        if (relogioSimulacao != null) relogioSimulacao.PararRelogio();
    }

    // --- LÓGICA DO PEIXE PRINCIPAL ---

    IEnumerator CicloDeVida()
    {
        while (simulando) 
        {
            AtualizarIndiceMinuto(); 
            
            if (timelineComportamento.Count == 0) yield break;

            DadosMinuto dadosAtuais;
            if (indiceMinutoAtual < timelineComportamento.Count)
                dadosAtuais = timelineComportamento[indiceMinutoAtual];
            else
                dadosAtuais = timelineComportamento[timelineComportamento.Count - 1];

            AtualizarPainelVariaveis(dadosAtuais);

            bool deveMover = Random.value < dadosAtuais.chanceEstarMovendo;

            if (deveMover)
            {
                int zona = EscolherZonaAlvo(dadosAtuais);
                Vector3 destino = GerarDestinoNaZona(zona);
                
                float velocidadeBruta;
                if (zona >= 3 && dadosAtuais.velocidadeMaxima > dadosAtuais.velocidadeMedia * 1.5f)
                    velocidadeBruta = dadosAtuais.velocidadeMaxima; 
                else
                    velocidadeBruta = Random.Range(dadosAtuais.velocidadeMedia * 0.9f, dadosAtuais.velocidadeMedia * 1.1f);

                yield return StartCoroutine(NadarPara(destino, velocidadeBruta));
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
                tempoSimulacao += 0.5f; 
            }
        }
    }

    IEnumerator NadarPara(Vector3 destino, float velocidadeBruta)
    {
        Vector3 inicio = transform.position;
        float distancia = Vector3.Distance(inicio, destino);
        
        if(distancia < 0.1f) yield break;

        float velocidadeUnity = velocidadeBruta * fatorDeEscala; 
        if(velocidadeUnity < 0.1f) velocidadeUnity = 0.1f; 

        float duracao = distancia / velocidadeUnity;
        float t = 0f;

        FlipSprite(destino.x);

        while (t < duracao)
        {
            float dt = Time.deltaTime; 
            if(dt == 0) dt = 0.016f; 

            t += dt;
            tempoSimulacao += dt; 
            float progress = t / duracao;

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            transform.position = Vector3.Lerp(inicio, destino, smoothProgress);
            yield return null;
        }
        transform.position = destino;
    }

    void FlipSprite(float destinoX)
    {
        if (Mathf.Abs(destinoX - transform.position.x) < 0.5f) return;
        float alvoX = (destinoX < transform.position.x) ? -escalaXOriginal : escalaXOriginal;
        if (Mathf.Sign(transform.localScale.x) == Mathf.Sign(alvoX) && Mathf.Abs(transform.localScale.x) > 0.1f) return;

        if (rotinaVirada != null) StopCoroutine(rotinaVirada);
        rotinaVirada = StartCoroutine(AnimarVirada(alvoX, transform));
    }

    // --- LÓGICA DO PEIXE FANTASMA ---

    IEnumerator CicloDeVidaFantasma()
    {
        while (simulando) 
        {
            if (timelineFantasma.Count == 0) yield break;

            DadosMinuto dadosFantasma;
            if (indiceMinutoAtual < timelineFantasma.Count)
                dadosFantasma = timelineFantasma[indiceMinutoAtual];
            else
                dadosFantasma = timelineFantasma[timelineFantasma.Count - 1];

            bool deveMover = Random.value < dadosFantasma.chanceEstarMovendo;

            if (deveMover)
            {
                int zona = EscolherZonaAlvo(dadosFantasma);
                Vector3 destino = GerarDestinoNaZona(zona);
                
                float velocidadeBruta;
                if (zona >= 3 && dadosFantasma.velocidadeMaxima > dadosFantasma.velocidadeMedia * 1.5f)
                    velocidadeBruta = dadosFantasma.velocidadeMaxima; 
                else
                    velocidadeBruta = Random.Range(dadosFantasma.velocidadeMedia * 0.9f, dadosFantasma.velocidadeMedia * 1.1f);

                yield return StartCoroutine(NadarParaFantasma(destino, velocidadeBruta));
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    IEnumerator NadarParaFantasma(Vector3 destino, float velocidadeBruta)
    {
        Vector3 inicio = peixeFantasma.transform.position;
        float distancia = Vector3.Distance(inicio, destino);
        
        if(distancia < 0.1f) yield break;

        float velocidadeUnity = velocidadeBruta * fatorDeEscala; 
        if(velocidadeUnity < 0.1f) velocidadeUnity = 0.1f; 

        float duracao = distancia / velocidadeUnity;
        float t = 0f;

        FlipSpriteFantasma(destino.x);

        while (t < duracao)
        {
            float dt = Time.deltaTime; 
            if(dt == 0) dt = 0.016f; 
            t += dt;
            float progress = t / duracao;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            peixeFantasma.transform.position = Vector3.Lerp(inicio, destino, smoothProgress);
            yield return null;
        }
        peixeFantasma.transform.position = destino;
    }

    void FlipSpriteFantasma(float destinoX)
    {
        if (Mathf.Abs(destinoX - peixeFantasma.transform.position.x) < 0.5f) return;
        float alvoX = (destinoX < peixeFantasma.transform.position.x) ? -escalaXOriginal : escalaXOriginal;
        if (Mathf.Sign(peixeFantasma.transform.localScale.x) == Mathf.Sign(alvoX) && Mathf.Abs(peixeFantasma.transform.localScale.x) > 0.1f) return;

        if (rotinaViradaFantasma != null) StopCoroutine(rotinaViradaFantasma);
        rotinaViradaFantasma = StartCoroutine(AnimarVirada(alvoX, peixeFantasma.transform));
    }

    // --- FUNÇÕES COMPARTILHADAS ---

    IEnumerator AnimarVirada(float alvoX, Transform alvoTransform)
    {
        float tempoVirada = 0.12f; 
        float t = 0f;
        float startX = alvoTransform.localScale.x;
        Vector3 scale = alvoTransform.localScale;

        while (t < tempoVirada)
        {
            float dt = Time.deltaTime; 
            if(dt == 0) dt = 0.016f; 
            t += dt;
            scale.x = Mathf.Lerp(startX, alvoX, t / tempoVirada);
            alvoTransform.localScale = scale;
            yield return null;
        }
        
        scale.x = alvoX;
        alvoTransform.localScale = scale;
    }
    
    void AtualizarIndiceMinuto() 
    { 
        indiceMinutoAtual = Mathf.FloorToInt(tempoSimulacao / 60f); 
    }

    void ConfigurarZonas()
    {
        limitesZonasY = new float[5];
        limitesZonasY[0] = -alturaAguaTotal/2;      
        limitesZonasY[1] = -alturaAguaTotal/4;      
        limitesZonasY[2] = 0f;                      
        limitesZonasY[3] = alturaAguaTotal/4;       
        limitesZonasY[4] = alturaAguaTotal/2;       
    }

    int EscolherZonaAlvo(DadosMinuto dados)
    {
        bool bloqueado = tempoSimulacao < tempoDeLatencia;
        float p1 = dados.probFundo1;
        float p2 = dados.probFundo2;
        float p3 = bloqueado ? 0 : dados.probTopo3;
        float p4 = bloqueado ? 0 : dados.probTopo4;
        
        float total = p1 + p2 + p3 + p4;
        if(total <= 0) return 1;

        float r = Random.value * total;
        if (r < p1) return 1;
        if (r < p1 + p2) return 2;
        if (r < p1 + p2 + p3) return 3;
        return 4;
    }

    Vector3 GerarDestinoNaZona(int zonaID)
    {
        float minY = limitesZonasY[zonaID - 1];
        float maxY = limitesZonasY[zonaID];
        
        float yRelativo = Random.Range(minY, maxY);
        float yFinal = 0;

        if(linhaCentral != null) yFinal = linhaCentral.position.y + yRelativo;
        
        float xFinal = Random.Range(limiteMinX, limiteMaxX);
        return new Vector3(xFinal, yFinal, transform.position.z);
    }

    // --- BANCOS DE DADOS ---

    void CarregarDadosNormais()
    {
        tempoDeLatencia = 215f; 
        if(timelineComportamento == null) timelineComportamento = new List<DadosMinuto>();
        timelineComportamento.Clear();

        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "0-1", probFundo1 = 0.73f, probFundo2 = 0.27f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.05f, velocidadeMaxima = 2.05f, chanceEstarMovendo = 0.53f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "1-2", probFundo1 = 0.60f, probFundo2 = 0.40f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 3.26f, velocidadeMaxima = 5.50f, chanceEstarMovendo = 0.65f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "2-3", probFundo1 = 0.52f, probFundo2 = 0.43f, probTopo3 = 0.05f, probTopo4 = 0f, velocidadeMedia = 8.05f, velocidadeMaxima = 82.81f, chanceEstarMovendo = 0.90f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "3-4", probFundo1 = 0.48f, probFundo2 = 0.45f, probTopo3 = 0.07f, probTopo4 = 0f, velocidadeMedia = 4.05f, velocidadeMaxima = 53.00f, chanceEstarMovendo = 0.85f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "4-5", probFundo1 = 0.30f, probFundo2 = 0.60f, probTopo3 = 0.10f, probTopo4 = 0f, velocidadeMedia = 4.35f, velocidadeMaxima = 47.55f, chanceEstarMovendo = 0.93f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "5-6", probFundo1 = 0.22f, probFundo2 = 0.35f, probTopo3 = 0.35f, probTopo4 = 0.08f, velocidadeMedia = 7.38f, velocidadeMaxima = 66.04f, chanceEstarMovendo = 0.96f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "6-7", probFundo1 = 0.32f, probFundo2 = 0.38f, probTopo3 = 0.30f, probTopo4 = 0f, velocidadeMedia = 6.35f, velocidadeMaxima = 29.00f, chanceEstarMovendo = 0.96f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "7-8", probFundo1 = 0.33f, probFundo2 = 0.28f, probTopo3 = 0.30f, probTopo4 = 0.09f, velocidadeMedia = 5.80f, velocidadeMaxima = 18.50f, chanceEstarMovendo = 1.0f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "8-9", probFundo1 = 0.22f, probFundo2 = 0.43f, probTopo3 = 0.20f, probTopo4 = 0.15f, velocidadeMedia = 6.20f, velocidadeMaxima = 13.50f, chanceEstarMovendo = 0.93f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "9-10", probFundo1 = 0.33f, probFundo2 = 0.55f, probTopo3 = 0.07f, probTopo4 = 0.05f, velocidadeMedia = 5.89f, velocidadeMaxima = 7.25f, chanceEstarMovendo = 0.95f });
    }

    void CarregarDadosAlcool()
    {
        tempoDeLatencia = 215f; 
        if(timelineComportamento == null) timelineComportamento = new List<DadosMinuto>();
        timelineComportamento.Clear();

        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "0-1", probFundo1 = 0.68f, probFundo2 = 0.32f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.50f, velocidadeMaxima = 5.91f, chanceEstarMovendo = 0.78f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "1-2", probFundo1 = 0.38f, probFundo2 = 0.48f, probTopo3 = 0.14f, probTopo4 = 0f, velocidadeMedia = 3.50f, velocidadeMaxima = 9.05f, chanceEstarMovendo = 0.95f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "2-3", probFundo1 = 0.23f, probFundo2 = 0.27f, probTopo3 = 0.37f, probTopo4 = 0.13f, velocidadeMedia = 5.45f, velocidadeMaxima = 41.05f, chanceEstarMovendo = 0.95f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "3-4", probFundo1 = 0.00f, probFundo2 = 0.22f, probTopo3 = 0.48f, probTopo4 = 0.30f, velocidadeMedia = 6.53f, velocidadeMaxima = 66.10f, chanceEstarMovendo = 1.00f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "4-5", probFundo1 = 0.07f, probFundo2 = 0.47f, probTopo3 = 0.35f, probTopo4 = 0.11f, velocidadeMedia = 6.65f, velocidadeMaxima = 45.00f, chanceEstarMovendo = 0.92f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "5-6", probFundo1 = 0.20f, probFundo2 = 0.33f, probTopo3 = 0.33f, probTopo4 = 0.14f, velocidadeMedia = 4.59f, velocidadeMaxima = 36.50f, chanceEstarMovendo = 0.88f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "6-7", probFundo1 = 0.07f, probFundo2 = 0.32f, probTopo3 = 0.46f, probTopo4 = 0.15f, velocidadeMedia = 6.54f, velocidadeMaxima = 19.77f, chanceEstarMovendo = 0.95f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "7-8", probFundo1 = 0.00f, probFundo2 = 0.43f, probTopo3 = 0.38f, probTopo4 = 0.19f, velocidadeMedia = 7.02f, velocidadeMaxima = 58.05f, chanceEstarMovendo = 0.85f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "8-9", probFundo1 = 0.13f, probFundo2 = 0.40f, probTopo3 = 0.30f, probTopo4 = 0.17f, velocidadeMedia = 6.05f, velocidadeMaxima = 36.54f, chanceEstarMovendo = 0.92f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "9-10", probFundo1 = 0.12f, probFundo2 = 0.35f, probTopo3 = 0.47f, probTopo4 = 0.06f, velocidadeMedia = 4.56f, velocidadeMaxima = 21.04f, chanceEstarMovendo = 0.93f });
    }

    void CarregarDadosAlcoolAlto() 
    {
        tempoDeLatencia = 215f; 
        if(timelineComportamento == null) timelineComportamento = new List<DadosMinuto>();
        timelineComportamento.Clear();

        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "0-1", probFundo1 = 0.97f, probFundo2 = 0.03f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 1.85f, velocidadeMaxima = 3.20f, chanceEstarMovendo = 0.17f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "1-2", probFundo1 = 0.82f, probFundo2 = 0.18f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 1.72f, velocidadeMaxima = 3.02f, chanceEstarMovendo = 0.25f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "2-3", probFundo1 = 0.77f, probFundo2 = 0.23f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 1.98f, velocidadeMaxima = 3.50f, chanceEstarMovendo = 0.23f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "3-4", probFundo1 = 0.53f, probFundo2 = 0.47f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.68f, velocidadeMaxima = 3.10f, chanceEstarMovendo = 0.23f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "4-5", probFundo1 = 0.68f, probFundo2 = 0.32f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.02f, velocidadeMaxima = 3.84f, chanceEstarMovendo = 0.30f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "5-6", probFundo1 = 0.63f, probFundo2 = 0.37f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.02f, velocidadeMaxima = 2.80f, chanceEstarMovendo = 0.57f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "6-7", probFundo1 = 0.52f, probFundo2 = 0.48f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.36f, velocidadeMaxima = 3.01f, chanceEstarMovendo = 0.48f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "7-8", probFundo1 = 0.47f, probFundo2 = 0.43f, probTopo3 = 0.10f, probTopo4 = 0f, velocidadeMedia = 3.02f, velocidadeMaxima = 16.50f, chanceEstarMovendo = 0.75f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "8-9", probFundo1 = 0.30f, probFundo2 = 0.52f, probTopo3 = 0.15f, probTopo4 = 0.03f, velocidadeMedia = 3.88f, velocidadeMaxima = 21.62f, chanceEstarMovendo = 0.77f });
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "9-10", probFundo1 = 0.45f, probFundo2 = 0.35f, probTopo3 = 0.13f, probTopo4 = 0.07f, velocidadeMedia = 3.05f, velocidadeMaxima = 14.50f, chanceEstarMovendo = 0.63f });
    }

    void CarregarDadosFantasma()
    {
        if(timelineFantasma == null) timelineFantasma = new List<DadosMinuto>();
        timelineFantasma.Clear();

        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "0-1", probFundo1 = 0.73f, probFundo2 = 0.27f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.05f, velocidadeMaxima = 2.05f, chanceEstarMovendo = 0.53f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "1-2", probFundo1 = 0.60f, probFundo2 = 0.40f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 3.26f, velocidadeMaxima = 5.50f, chanceEstarMovendo = 0.65f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "2-3", probFundo1 = 0.52f, probFundo2 = 0.43f, probTopo3 = 0.05f, probTopo4 = 0f, velocidadeMedia = 8.05f, velocidadeMaxima = 82.81f, chanceEstarMovendo = 0.90f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "3-4", probFundo1 = 0.48f, probFundo2 = 0.45f, probTopo3 = 0.07f, probTopo4 = 0f, velocidadeMedia = 4.05f, velocidadeMaxima = 53.00f, chanceEstarMovendo = 0.85f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "4-5", probFundo1 = 0.30f, probFundo2 = 0.60f, probTopo3 = 0.10f, probTopo4 = 0f, velocidadeMedia = 4.35f, velocidadeMaxima = 47.55f, chanceEstarMovendo = 0.93f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "5-6", probFundo1 = 0.22f, probFundo2 = 0.35f, probTopo3 = 0.35f, probTopo4 = 0.08f, velocidadeMedia = 7.38f, velocidadeMaxima = 66.04f, chanceEstarMovendo = 0.96f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "6-7", probFundo1 = 0.32f, probFundo2 = 0.38f, probTopo3 = 0.30f, probTopo4 = 0f, velocidadeMedia = 6.35f, velocidadeMaxima = 29.00f, chanceEstarMovendo = 0.96f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "7-8", probFundo1 = 0.33f, probFundo2 = 0.28f, probTopo3 = 0.30f, probTopo4 = 0.09f, velocidadeMedia = 5.80f, velocidadeMaxima = 18.50f, chanceEstarMovendo = 1.0f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "8-9", probFundo1 = 0.22f, probFundo2 = 0.43f, probTopo3 = 0.20f, probTopo4 = 0.15f, velocidadeMedia = 6.20f, velocidadeMaxima = 13.50f, chanceEstarMovendo = 0.93f });
        timelineFantasma.Add(new DadosMinuto { nomeMinuto = "9-10", probFundo1 = 0.33f, probFundo2 = 0.55f, probTopo3 = 0.07f, probTopo4 = 0.05f, velocidadeMedia = 5.89f, velocidadeMaxima = 7.25f, chanceEstarMovendo = 0.95f });
    }
}