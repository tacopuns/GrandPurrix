using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpriteDirectionalController : MonoBehaviour
{
    public Rigidbody2D body;
    public SpriteRenderer spriteRenderer;
    
    public List<Sprite> wSprites;
    public List<Sprite> aSprites;
    public List<Sprite> dSprites;

    Vector2 direction;

    //private PlayerControls controls;

    private InputActionAsset inputAsset;
    private InputActionMap gameplay;
    private InputAction ChangeSprite;


    private void Awake()
    {
        //controls = new PlayerControls();

        inputAsset = this.GetComponent<PlayerInput>().actions;
        gameplay = inputAsset.FindActionMap("Gameplay");

        gameplay.FindAction("ChangeSprite").started += ctx => GetSpriteDirection();

        gameplay.FindAction("ChangeSprite").canceled += ctx => GetSpriteDirection();

        
        //controls.Gameplay.ChangeSprite.performed += ctx => GetSpriteDirection();

        //direction = controls.Gameplay.ChangeSprite.ReadValue<Vector2>();
    }
    
    private void OnEnable()
    {
        gameplay.Enable();
    }

    private void OnDisable()
    {
        gameplay.Disable();
    }

    
    void Update()
    {
        //direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        //direction = gameplay.Gameplay.ChangeSprite.ReadValue<Vector2>();

        direction =  gameplay.FindAction("ChangeSprite").ReadValue<Vector2>();

        handleSpriteFlip();
        GetSpriteDirection();

        List<Sprite> directionSprites = GetSpriteDirection();
        if (directionSprites !=null)
        {
            spriteRenderer.sprite = directionSprites[0];
        }
        else
        {

        }
    }

    void handleSpriteFlip()
    {
        if(!spriteRenderer.flipX && direction.x <0)
        {
            spriteRenderer.flipX = true;
        }
        else if (spriteRenderer.flipX && direction.x > 0 )
        {
            spriteRenderer.flipX = false;
        }
    }

    List<Sprite> GetSpriteDirection()
    {
        List<Sprite> selectedSprites = null;

        if(direction.y > 0)
        {
            if(Mathf.Abs(direction.x) > 0)
            {
                selectedSprites = dSprites;
            }
                else
                {
                    selectedSprites = wSprites;
                }
        }

        else if(direction.y < 0)
            {
            if (Mathf.Abs(direction.x) > 0)
            {
                selectedSprites = dSprites;
            }
                else
                {  
                    selectedSprites = aSprites;
                }

            }

        
            else
            {
                if (Mathf.Abs(direction.x) > 0)
                {
                    selectedSprites = wSprites;
                }
            }
            
            

            return selectedSprites;   
        
    }


}
