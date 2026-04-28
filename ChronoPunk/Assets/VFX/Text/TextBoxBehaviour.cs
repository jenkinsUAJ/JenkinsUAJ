using UnityEngine;

[RequireComponent(typeof(Animator))]
public class TextAppearBehaviour : MonoBehaviour
{
    public AnimationClip appearAnimation;
    public AnimationClip dissapearAnimation;
    private Animator _animator;


    public void OnPlayerEnter()
    {
        _animator.Play(appearAnimation.name);
    }

    public void OnPlayerExit()
    {
        _animator.Play(dissapearAnimation.name);
    }

    void Start()
    {
        _animator = GetComponent<Animator>();
    }
}
