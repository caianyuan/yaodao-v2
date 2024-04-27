using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GengZhaoSound : MonoBehaviour
{
    [Header("Footsteps")] public List<AudioClip> grassFS;
    public List<AudioClip> rockFS;
    public List<AudioClip> woodFS;

    enum FSMaterial
    {
        Grass,
        Rock,
        Wood,
        Empty
    }

    private AudioSource footstepSource;


    // Start is called before the first frame update
    void Start()
    {
        //footstep sound
        footstepSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
    }


    //Material detect
    private FSMaterial SurfaceSelect()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, -Vector3.up);
        Material surfaceMaterial;

        if (Physics.Raycast(ray, out hit, 1.0f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            Renderer surfaceRenderer = hit.collider.GetComponentInChildren<Renderer>();
            if (surfaceRenderer)
            {
                surfaceMaterial = surfaceRenderer ? surfaceRenderer.sharedMaterial : null;
                if (surfaceMaterial.name.Contains("Grass"))
                {
                    return FSMaterial.Grass;
                }
                else if (surfaceMaterial.name.Contains("Stone"))
                {
                    return FSMaterial.Rock;
                }
                else if (surfaceMaterial.name.Contains("Wood"))
                {
                    return FSMaterial.Wood;
                }
                else
                {
                    return FSMaterial.Empty;
                }
            }
        }

        return FSMaterial.Empty;
    }

    void PlayFootstep()
    {
        AudioClip clip = null;

        FSMaterial surface = SurfaceSelect();

        switch (surface)
        {
            case FSMaterial.Grass:
                clip = grassFS[0];
                break;
            case FSMaterial.Rock:
                clip = rockFS[0];
                break;
            case FSMaterial.Wood:
                clip = woodFS[0];
                break;
            default:
                break;
        }

        if (surface != FSMaterial.Empty)
        {
            footstepSource.clip = clip;
            footstepSource.volume = Random.Range(0.02f, 0.05f);
            footstepSource.pitch = Random.Range(0.8f, 1.2f);
            footstepSource.Play();
        }
    }
}