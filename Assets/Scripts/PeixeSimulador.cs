using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Header("--- VERSÃO FINAL (VIRADA SUAVE) ---")]
    public bool iniciarAutomaticamente = false; 

    [Header("Configuração de Espaço")]
    public Transform linhaCentral; 
    public float alturaAguaTotal = 8f; 
    public float limiteMinX = -8f; 
    public float limiteMaxX = 8f;
    public float tempoDeLatencia = 215f; 

    [Header("Escala da Simulação")]
    [Tooltip("Ajuste isso para casar os cm/s reais com o tamanho do seu desenho na Unity.")]
    public float fatorDeEscala = 0.08f;

    [Header("Dados")]
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
        // Salva o tamanho original do seu peixe (no seu caso, 200) para não perder a proporção
        escalaXOriginal = Mathf.Abs(transform.localScale.x);
        
        timelineComportamento = new List<DadosMinuto>();
        ConfigurarZonas();
        CarregarDadosCompletos(); 
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

        if (iniciarAutomaticamente) IniciarSimulacao();
    }

    public void IniciarSimulacao()
    {
        if (timelineComportamento == null || timelineComportamento.Count == 0)
             CarregarDadosCompletos();

        if (!simulando)
        {
            simulando = true;
            tempoSimulacao = 0f;
            
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
        Debug.Log("Peixe parado pelo fim da simulação.");
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

        // Agora a virada acontece junto com o nado de forma orgânica
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

    // --- SISTEMA DE VIRADA SUAVE ---

    void FlipSprite(float destinoX)
    {
        // 1. Evita "micro-viradas" se o peixe for nadar quase reto para cima ou para baixo
        if (Mathf.Abs(destinoX - transform.position.x) < 0.5f) return;

        // 2. Define para qual lado ele tem que olhar
        float alvoX = (destinoX < transform.position.x) ? -escalaXOriginal : escalaXOriginal;

        // 3. Se a direção já é a mesma, não faz nada
        if (Mathf.Sign(transform.localScale.x) == Mathf.Sign(alvoX) && Mathf.Abs(transform.localScale.x) > 0.1f) return;

        // 4. Inicia a animação de rotação em paralelo com o movimento
        if (rotinaVirada != null) StopCoroutine(rotinaVirada);
        rotinaVirada = StartCoroutine(AnimarVirada(alvoX));
    }

    IEnumerator AnimarVirada(float alvoX)
    {
        float tempoVirada = 0.12f; // Tempo perfeito para imitar um giro rápido do corpo
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

    // --- FUNÇÕES AUXILIARES ---
    
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

    void CarregarDadosCompletos()
    {
        if(timelineComportamento == null) timelineComportamento = new List<DadosMinuto>();
        timelineComportamento.Clear();

        // 0-1
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "0-1", probFundo1 = 0.73f, probFundo2 = 0.27f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 2.05f, velocidadeMaxima = 2.05f, chanceEstarMovendo = 0.53f });
        // 1-2
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "1-2", probFundo1 = 0.60f, probFundo2 = 0.40f, probTopo3 = 0f, probTopo4 = 0f, velocidadeMedia = 3.26f, velocidadeMaxima = 5.50f, chanceEstarMovendo = 0.65f });
        // 2-3
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "2-3", probFundo1 = 0.52f, probFundo2 = 0.43f, probTopo3 = 0.05f, probTopo4 = 0f, velocidadeMedia = 8.05f, velocidadeMaxima = 82.81f, chanceEstarMovendo = 0.90f });
        // 3-4
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "3-4", probFundo1 = 0.48f, probFundo2 = 0.45f, probTopo3 = 0.07f, probTopo4 = 0f, velocidadeMedia = 4.05f, velocidadeMaxima = 53.00f, chanceEstarMovendo = 0.85f });
        // 4-5
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "4-5", probFundo1 = 0.30f, probFundo2 = 0.60f, probTopo3 = 0.10f, probTopo4 = 0f, velocidadeMedia = 4.35f, velocidadeMaxima = 47.55f, chanceEstarMovendo = 0.93f });
        // 5-6
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "5-6", probFundo1 = 0.22f, probFundo2 = 0.35f, probTopo3 = 0.35f, probTopo4 = 0.08f, velocidadeMedia = 7.38f, velocidadeMaxima = 66.04f, chanceEstarMovendo = 0.96f });
        // 6-7
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "6-7", probFundo1 = 0.32f, probFundo2 = 0.38f, probTopo3 = 0.30f, probTopo4 = 0f, velocidadeMedia = 6.35f, velocidadeMaxima = 29.00f, chanceEstarMovendo = 0.96f });
        // 7-8
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "7-8", probFundo1 = 0.33f, probFundo2 = 0.28f, probTopo3 = 0.30f, probTopo4 = 0.09f, velocidadeMedia = 5.80f, velocidadeMaxima = 18.50f, chanceEstarMovendo = 1.0f });
        // 8-9
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "8-9", probFundo1 = 0.22f, probFundo2 = 0.43f, probTopo3 = 0.20f, probTopo4 = 0.15f, velocidadeMedia = 6.20f, velocidadeMaxima = 13.50f, chanceEstarMovendo = 0.93f });
        // 9-10
        timelineComportamento.Add(new DadosMinuto { nomeMinuto = "9-10", probFundo1 = 0.33f, probFundo2 = 0.55f, probTopo3 = 0.07f, probTopo4 = 0.05f, velocidadeMedia = 5.89f, velocidadeMaxima = 7.25f, chanceEstarMovendo = 0.95f });
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