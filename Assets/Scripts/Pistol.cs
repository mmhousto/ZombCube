using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : MonoBehaviour
{
    public GameObject pistol; // Pistol Object
    public Transform muzzle; // Pistol Muzzle
    public GameObject millimeterBullets; // 9mm Bullet Prefab
    public Animator animator; // Animator
    private AudioClip pistolShot; // Pistol Fire Sound
    // public ParticleSystem muzzleFlash; // Muzzle Flash Particle Effect
}
