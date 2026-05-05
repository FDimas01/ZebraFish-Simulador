using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.UI; 

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
    [Header("--- VERSÃO FINAL (PERFIS MÚLTIPLOS) ---")]
    public bool iniciarAutomaticamente = false; 

    [Header("UI (Interface)")]
    public TextMeshProUGUI textoModoAtual; 
    public Button btnNormal; 
    public Button btnAlcool; 
    public TextMeshProUGUI textoVariaveis; 

    [Header("Sistemas Externos")]
    public RelogioSimulacao relogioSimulacao; // <-- ADICIONADO: Referência do Relógio

    [Header("Configuração de Espaço")]
    public Transform linhaCentral; 
    public float alturaAguaTotal = 8f; 
    public float limiteMinX = -8f; 
    public float limiteMaxX = 8f;
    public float tempoDeLatencia = 215f; 

    [Header("Escala da Simulação")]
    [Tooltip("Ajuste isso para casar os cm/s reais com o tamanho do seu desenho na Unity.")]
    public float fatorDeEscala = 0.08f;

    [Header("Dados (Visualização)")]
    public List<DadosMinuto> timelineComportamento;

    // Estado Interno
    private float tempoSimulacao = 0f;
    private bool simulando = false;
    private int indiceMinutoAtual = 0;
    private Animator anim;
    private float[] limitesZonasY; 
    
    // Novas variáveis para controlar a virada
    private float escalaXOriginal;
    private Coroutine rotinaVirada;

    void Awake()
    {
        escalaXOriginal = Mathf.Abs(transform.localScale.x);
        timelineComportamento = new List<DadosMinuto>();
        ConfigurarZonas();
        CarregarDadosNormais(); 
    }

    void Start()
    {
        if (Time.timeScale == 0) Time.timeScale = 1f;

        anim = GetComponent<Animator>();
        if (anim != null && anim.enabled) anim.enabled = false;

        if (linhaCentral == null)
        {
            Debug.LogError("ERRO: Arraste o objeto ReferenciaCentro para o campo Linha Central!");
            return;
        }

        if (textoModoAtual != null) textoModoAtual.text = "Modo: Controle Negativo (Normal)";
        
        if (btnNormal != null) btnNormal.interactable = false;
        if (btnAlcool != null) btnAlcool.interactable = true;

        if (textoVariaveis != null) textoVariaveis.text = "Aguardando início da simulação...";

        if (iniciarAutomaticamente) IniciarSimulacao();
    }

    // --- MÉTODOS PARA OS BOTÕES DA UI ---

    public void IniciarSimulacaoNormal()
    {
        PararSimulacao();
        CarregarDadosNormais();
        
        if (textoModoAtual != null) textoModoAtual.text = "Modo: Controle Negativo (Normal)";
        
        if (btnNormal != null) btnNormal.interactable = false;
        if (btnAlcool != null) btnAlcool.interactable = true;

        IniciarSimulacao();
        Debug.Log("Simulação reiniciada: Controle Negativo (Normal)");
    }

    public void IniciarSimulacaoAlcool()
    {
        PararSimulacao();
        CarregarDadosAlcool();
        
        if (textoModoAtual != null) textoModoAtual.text = "Modo: Álcool 0,5%";

        if (btnNormal != null) btnNormal.interactable = true;
        if (btnAlcool != null) btnAlcool.interactable = false;

        IniciarSimulacao();
        Debug.Log("Simulação reiniciada: Exposição ao Álcool 0,5%");
    }

    // ------------------------------------

    public void IniciarSimulacao()
    {
        if (timelineComportamento == null || timelineComportamento.Count == 0)
             CarregarDadosNormais();

        if (!simulando)
        {
            simulando = true;
            tempoSimulacao = 0f;
            
            // <-- CHAMA O RELÓGIO PARA ZERAR E COMEÇAR A CONTAR
            if (relogioSimulacao != null) 
            {
                relogioSimulacao.IniciarRelogio();
            }
            
            if (linhaCentral != null)
            {
                transform.position = GerarDestinoNaZona(1);
            }
            
            StartCoroutine(CicloDeVida());
        }
    }

    public void PararSimulacao()
    {
        simulando = false;
        StopAllCoroutines(); 
        
        // <-- CHAMA O RELÓGIO PARA PARAR DE CONTAR
        if (relogioSimulacao != null)
        {
            relogioSimulacao.PararRelogio();
        }
    }

    void AtualizarPainelVariaveis(DadosMinuto dados)
    {
        if (textoVariaveis == null) return;

        textoVariaveis.text = 
            $"<b>Minuto Atual:</b> {dados.nomeMinuto}\n\n" +
            $"<b>Prob. Fundo 1:</b> {(dados.probFundo1 * 100).ToString("F0")}%\n" +
            $"<b>Prob. Fundo 2:</b> {(dados.probFundo2 * 100).ToString("F0")}%\n" +
            $"<b>Prob. Topo 3:</b> {(dados.probTopo3 * 100).ToString("F0")}%\n" +
            $"<b>Prob. Topo 4:</b> {(dados.probTopo4 * 100).ToString("F0")}%\n" +
            $"<b>Veloc. Média:</b> {dados.velocidadeMedia} cm/s\n" +
            $"<b>Veloc. Máx:</b> {dados.velocidadeMaxima} cm/s\n" +
            $"<b>Chance de Mover:</b> {(dados.chanceEstarMovendo * 100).ToString("F0")}%";
    }

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
            Vector3 novaPos = Vector3.Lerp(inicio, destino, smoothProgress);

            transform.position = novaPos;
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
        rotinaVirada = StartCoroutine(AnimarVirada(alvoX));
    }

    IEnumerator AnimarVirada(float alvoX)
    {
        float tempoVirada = 0.12f; 
        float t = 0f;
        float startX = transform.localScale.x;
        Vector3 scale = transform.localScale;

        while (t < tempoVirada)
        {
            float dt = Time.deltaTime; 
            if(dt == 0) dt = 0.016f; 

            t += dt;
            scale.x = Mathf.Lerp(startX, alvoX, t / tempoVirada);
            transform.localScale = scale;
            yield return null;
        }
        
        scale.x = alvoX;
        transform.localScale = scale;
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

        if(linhaCentral != null)
             yFinal = linhaCentral.position.y + yRelativo;
        
        float xFinal = Random.Range(limiteMinX, limiteMaxX);
        
        return new Vector3(xFinal, yFinal, transform.position.z);
    }

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

    void OnDrawGizmos()
    {
        if (linhaCentral == null) return;
        float cy = linhaCentral.position.y;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(-10, cy - alturaAguaTotal/2, 0), new Vector3(10, cy - alturaAguaTotal/2, 0)); 
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(-10, cy + alturaAguaTotal/2, 0), new Vector3(10, cy + alturaAguaTotal/2, 0)); 
        Gizmos.color = Color.red;
        Vector3 cantoEsqSup = new Vector3(limiteMinX, cy + alturaAguaTotal/2, 0);
        Vector3 cantoDirSup = new Vector3(limiteMaxX, cy + alturaAguaTotal/2, 0);
        Vector3 cantoEsqInf = new Vector3(limiteMinX, cy - alturaAguaTotal/2, 0);
        Vector3 cantoDirInf = new Vector3(limiteMaxX, cy - alturaAguaTotal/2, 0);
        Gizmos.DrawLine(cantoEsqSup, cantoEsqInf); 
        Gizmos.DrawLine(cantoDirSup, cantoDirInf); 
    }
}