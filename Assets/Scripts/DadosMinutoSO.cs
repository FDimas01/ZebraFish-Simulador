using UnityEngine;

[CreateAssetMenu(fileName = "DadoMinuto", menuName = "app/DadoMinuto")]
public class DadosMinutoSO:ScriptableObject
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