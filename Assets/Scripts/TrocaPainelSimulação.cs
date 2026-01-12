using UnityEngine;

public class TrocaPainelSimulacao : MonoBehaviour
{
    [Header("Painéis")]
    public GameObject painelInicio;
    public GameObject painelSimulacao;
    public GameObject painelFim; // ARRASTE O PAINEL DE FIM AQUI

    [Header("Scripts")]
    public PeixeSimulador peixeSimulador;         
    public RelogioSimulacao relogioSimulador;     

    void Start()
    {
        // Garante que começa certo
        painelInicio.SetActive(true);
        painelSimulacao.SetActive(false);
        painelFim.SetActive(false);
    }

    // Botão "INICIAR"
    public void IniciarSimulacao()
    {
        painelInicio.SetActive(false);
        painelFim.SetActive(false);
        painelSimulacao.SetActive(true);

        if (peixeSimulador != null)
            peixeSimulador.IniciarSimulacao();

        if (relogioSimulador != null)
            relogioSimulador.IniciarRelogio();
    }

    // Chamado automaticamente quando o relógio bate 10 minutos
    public void FinalizarSimulacao()
    {
        Debug.Log("Simulação Finalizada!");
        
        // Para o peixe
        if (peixeSimulador != null)
            peixeSimulador.PararSimulacao();

        // Para o relógio
        if (relogioSimulador != null)
            relogioSimulador.PararRelogio();

        // Mostra o painel de fim
        painelFim.SetActive(true);
    }

    // Botão "MENU" (no painel de fim)
    public void VoltarAoMenu()
    {
        painelFim.SetActive(false);
        painelSimulacao.SetActive(false);
        painelInicio.SetActive(true);
    }
}