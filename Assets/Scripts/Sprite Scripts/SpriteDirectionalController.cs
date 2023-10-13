using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteDirectionalController : MonoBehaviour
{
    public Rigidbody2D body;
    public SpriteRenderer spriteRenderer;
    
    public List<Sprite> wSprites;
    public List<Sprite> aSprites;
    public List<Sprite> dSprites;

    Vector2 direction;

    private PlayerControls controls;

    
    void Update()
    {
        //direction = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        direction = controls.Gameplay.ChangeSprite.ReadValue<Vector2>();

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
