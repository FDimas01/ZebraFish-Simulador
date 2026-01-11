using UnityEngine;
using TMPro;

public class RelogioSimulacao : MonoBehaviour
{
    public TextMeshProUGUI textoRelogio;
    public float tempoDecorrido = 0f;
    public bool contando = false; // Agora começa parado

    void Update()
    {
        if (contando)
        {
            tempoDecorrido += Time.deltaTime;
            AtualizarRelogio();
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
