using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{

    [Header("Definiowanie dynamicznie")]
    public Rigidbody rigid;
    
    private Neighborhood neighborhood;

    void Awake() {
        neighborhood = GetComponent<Neighborhood>();
        rigid = GetComponent<Rigidbody>();

        //losowe polozenie poczatkowe
        pos = Random.insideUnitSphere * Spawner.S.spawnRadius;
        //losowa predkosc poczatkowa
        Vector3 vel = Random.onUnitSphere * Spawner.S.velocity;
        rigid.velocity = vel;

        LookAhead();

        //losowy kolor dla boida, lecz upewnij sie zeby nie byl zbyt ciemny
        Color randColor = Color.black;
        while(randColor.r + randColor.g + randColor.b < 1.0f){
            randColor = new Color(Random.value, Random.value, Random.value);
        }
        Renderer[] rends = gameObject.GetComponentsInChildren<Renderer>();
        foreach(Renderer r in rends){
            r.material.color = randColor;
        }
        TrailRenderer tRend = GetComponent<TrailRenderer>();
        tRend.material.SetColor("_TinTColor", randColor);
    }

    void LookAhead(){
        //Obraca boida w taki sposob aby patrzyl w kierunkuw  ktorym leci
        transform.LookAt(pos + rigid.velocity);
    }

    public Vector3 pos{
        get{return transform.position;}
        set{transform.position = value;}
    }


    //Funckja FixedUpdate jest wywoływana, gdy występuje aktualizacja parametrów fizycznych
    //(50 razy na sekunde)
    void FixedUpdate() {
        Vector3 vel = rigid.velocity;
        Spawner spn = Spawner.S;

        //UNIKANIE ZDERZENIA - unikaj sąsiadów, którzy są zbyt blisko
        Vector3 velAvoid = Vector3.zero;
        Vector3 tooClosePos = neighborhood.avgClosePos;
        //Jesli wynik jest rowny Vector3.zero nie musimy nic robic
        if(tooClosePos != Vector3.zero){
            velAvoid = pos - tooClosePos;
            velAvoid.Normalize();
            velAvoid *= spn.velocity;
        }

        //DOPASOWANIE PREDKOSCI - staraj sie meic predkosc taka sama jak sasiedzi
        Vector3 velAlign = neighborhood.avgVel;
        if(velAlign != Vector3.zero){
            velAlign.Normalize();
            velAlign *= spn.velocity;
        }

        //DAZENIE DO SRODKA STADA - przemieszczaj sie w kierunku centrum lokalnej grupy sasiadujacych boidow
        Vector3 velCenter = neighborhood.avgPos;
        if(velCenter != Vector3.zero){
            velCenter -= transform.position;
            velCenter.Normalize();
            velCenter *= spn.velocity;
        }

        //PRZYCIAGANIE - poruszanie sie w kierunku obiektu Attractor
        Vector3 delta = Attractor.POS - pos;
        //Sprawdzenie, czy Attractor przyciaga lub odpycha
        bool attracted = (delta.magnitude > spn.attractPushDist);
        Vector3 velAttract = delta.normalized * spn.velocity;

        //Zastosuj wszystkie wartosci predkosci
        float fdt = Time.fixedDeltaTime;
        if(velAvoid != Vector3.zero){
            vel = Vector3.Lerp(vel, velAvoid, spn.collAvoid*fdt);
        }
        else{
            if(velAlign != Vector3.zero){
                vel = Vector3.Lerp(vel, velAlign, spn.velMatching*fdt);
            }
            if(velCenter != Vector3.zero){
                vel = Vector3.Lerp(vel, velAlign, spn.flockCentering*fdt);
            }
            if(velAttract != Vector3.zero){
                if(attracted){
                    vel = Vector3.Lerp(vel, velAttract, spn.attractPull*fdt);
                }
                else{
                    vel = Vector3.Lerp(vel, -velAttract, spn.attractPush*fdt);
                }
            }
        }

        //Przypisz zmiennej vel predkosc z singletona Spawner
        vel = vel.normalized * spn.velocity;
        //Ostatecznie przypisz ją do komponentu Rigidbody
        rigid.velocity = vel;
        //Patrz w kierunku określonym przez wartosc velocity
        LookAhead();

    }
}
