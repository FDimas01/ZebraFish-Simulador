using UnityEngine;
using TMPro;

public class RelogioSimulacao : MonoBehaviour
{
    public TextMeshProUGUI textoRelogio;
    public float tempoDecorrido = 0f;
    public bool contando = false; 

    // Referência para podermos avisar quando acabar o tempo
    public TrocaPainelSimulacao controladorPainel; 

    void Update()
    {
        if (contando)
        {
            tempoDecorrido += Time.deltaTime;
            AtualizarRelogio();

            // VERIFICA SE CHEGOU AOS 10 MINUTOS (600 SEGUNDOS)
            if (tempoDecorrido >= 600f)
            {
                tempoDecorrido = 600f; // Trava em 10:00
                AtualizarRelogio(); // Atualiza visualmente uma ultima vez
                
                // Avisa o controlador para encerrar
                if(controladorPainel != null)
                {
                    controladorPainel.FinalizarSimulacao();
                }
                else
                {
                    Debug.LogError("Relógio não tem referência do ControladorPainel!");
                }
            }
        }
    }

    void AtualizarRelogio()
    {
        int minutos = Mathf.FloorToInt(tempoDecorrido / 60f);
        int segundos = Mathf.FloorToInt(tempoDecorrido % 60f);
        int milissegundos = Mathf.FloorToInt((tempoDecorrido * 1000f) % 1000f);

        textoRelogio.text = string.Format("{0:00}:{1:00}:{2:000}", minutos, segundos, milissegundos);
    }

    public void IniciarRelogio()
    {
        tempoDecorrido = 0f;
        contando = true;
    }

    public void PararRelogio()
    {
        contando = false;
    }
}