using UnityEngine;

public class TrocaPainelSimulacao : MonoBehaviour
{
    public GameObject painelInicio;
    public GameObject painelSimulacao;
    public PeixeSimulador peixeSimulador;         // Referência ao script do peixe
    public RelogioSimulacao relogioSimulador;     // Referência ao script do relógio (TextMeshPro)

    void Start()
    {
        painelInicio.SetActive(true);
        painelSimulacao.SetActive(false);
    }

    public void IniciarSimulacao()
    {
        painelInicio.SetActive(false);
        painelSimulacao.SetActive(true);

        if (peixeSimulador != null)
            peixeSimulador.IniciarSimulacao();

        if (relogioSimulador != null)
            relogioSimulador.IniciarRelogio();
    }
}
