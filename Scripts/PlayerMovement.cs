using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public GameEventsSO PlayerEvents;
    public Sprite DeathSprite;
    public GameObject BeatParticle;
    public GameObject DeathParticles;

    // sound
    private AudioSource AudioSource;
    public AudioClip[] HurtClips;

    public Material DamageMaterial;
    private Material OriginalMaterial;

    bool beatActive = false; //true during the beat, false otherwise

    float xMove;
    float yMove;
    [ SerializeField ] float _walkingSpeed;

    public int StartPoints; //Number of points you have at the start or after the combo 
    public int _points; // Total points this is is what gets printed to the style bar
    public int _pointIncrease; //how much to increase the points by when the player hits the beat
    public int DamageTaken; //Amount of points you lose type as a negative
    public int DazzlingDancePoints; //points used to strack whether you can dazzling dance or not 
    int DazzMax = 110;
    int DazzMin = 95;
    int DAZZPOINTS = 10;

    Rigidbody2D rb;

    State _movementState = State.Nothing;

    [ SerializeField ] string[] PlayerAnimations;
    Animator anim;
    float idleTimer = 0;
    float DamageTimer = 0;

    enum State
    {
        Nothing,
        Sliding,

        //Running,
    }

    Vector2 _nextPosition;
    Vector2 _oldPosition;

    //float _runTimer;

    // Start is called before the first frame update
    void Start()
    {
        OriginalMaterial = gameObject.GetComponent< SpriteRenderer >().material;
        AudioSource = gameObject.GetComponent<AudioSource>();

        rb = GetComponent< Rigidbody2D >();
        PlayerEvents.BeforeBeatEvent.AddListener( BeatWindowStart );
        PlayerEvents.AfterBeatEvent.AddListener( BeatWindowEnd );
        PlayerEvents.PointsChangedEvent.AddListener( OnPointsChanged );
        _points = StartPoints;
        //PlayerEvents.PointsChangedEvent.Invoke( StartPoints );
        PlayerEvents.GameOverEvent.AddListener( GameOver );
        anim = GetComponent< Animator >();
    }

    bool _gameOver;

    // Update is called once per frame
    void Update()
    {
        if ( !_gameOver )
        {
            CheckInput();
        }

        if ( DazzlingDancePoints > DazzMax)
        {
            DazzlingDancePoints = DazzMax;
        }
        if(DazzlingDancePoints < 0)
        {
            DazzlingDancePoints = 0;
        }
    }

    void FixedUpdate()
    {
        if ( !_gameOver )
        {
            MovePlayer();
        }
    }


    void GameOver()
    {
        GetComponent< Animator >().enabled = false;
        GetComponent< SpriteRenderer >().sprite = DeathSprite;
        transform.localScale = new Vector3( 0.10f, 0.10f, 1 );
        _gameOver = true;
    }

    void CheckInput()
    {
        xMove = Input.GetAxisRaw( "Horizontal" );
        yMove = Input.GetAxisRaw( "Vertical" );

        //Dazzling Dance inputs Begin
        if ( Input.GetKeyDown( KeyCode.C ))
        {
            if(Input.GetKeyDown( KeyCode.Q ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if ( Input.GetKeyDown( KeyCode.Q ))
        {
            if(Input.GetKeyDown( KeyCode.C ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if ( Input.GetKeyDown( KeyCode.Z ))
        {
            if(Input.GetKeyDown( KeyCode.E ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if ( Input.GetKeyDown( KeyCode.E ))
        {
            if(Input.GetKeyDown( KeyCode.Z ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if ( Input.GetKeyDown( KeyCode.Keypad3 ))
        {
            if(Input.GetKeyDown( KeyCode.Keypad7 ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }


        if ( Input.GetKeyDown( KeyCode.Keypad7 ))
        {
            if(Input.GetKeyDown( KeyCode.Keypad3 ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if ( Input.GetKeyDown( KeyCode.Keypad9 ))
        {
            if(Input.GetKeyDown( KeyCode.Keypad1 ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }
        
        if ( Input.GetKeyDown( KeyCode.Keypad1 ))
        {
            if(Input.GetKeyDown( KeyCode.Keypad9 ))
            {
                if ( DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if (Input.GetButtonDown("Up Left"))
        {
            if (Input.GetButtonDown("Down Right "))
            {
                if (DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if (Input.GetButtonDown("Down Right "))
        {
            if (Input.GetButtonDown("Up Left"))
            {
                if (DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if (Input.GetButtonDown("Up Right"))
        {
            if (Input.GetButtonDown("Down Left"))
            {
                if (DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        if (Input.GetButtonDown("Down Left"))
        {
            if (Input.GetButtonDown("Up Right"))
            {
                if (DazzlingDancePoints >= DazzMin)
                {
                    DazzlingDance();
                }
            }
        }

        //Dazzling Dance Inputs end

        if ( Input.GetButtonDown( "Horizontal" ) || Input.GetButtonDown( "Vertical" ) )
        {
            //will call only on the first frame the player presses the button, ideally would only be called when they use the tap but can also happen when they do the walking or sprinting
            if ( beatActive )
            {
                // creating feedbackyou are in beat - niko
                Instantiate( BeatParticle, gameObject.transform.position, Quaternion.identity );
                IncreasePoints();
                beatActive = false; //makes it so you can only gain points once per beat
            }
        }


        switch ( _movementState )
        {
            // If player is not currently moving,
            // check if they are moving and if so
            // transition them into sliding
            case State.Nothing:
                if ( xMove != 0 || yMove != 0 )
                {
                    _nextPosition = transform.position + new Vector3( xMove * TilesToMove, yMove * TilesToMove );
                    PlayAnimation();
                    if ( Mathf.Abs( _nextPosition.x ) >= 10 || Mathf.Abs( _nextPosition.y ) >= 10 )
                    {
                        break;
                    }

                    _movementState = State.Sliding;
                    _oldPosition = transform.position;
                }

                break;

            // If the player is currently sliding
            case State.Sliding:

                // if the player is at the point where they 
                // wanted to slide to then transition to 
                // doing nothing
                if ( Mathf.Approximately( transform.position.x, _nextPosition.x ) && Mathf.Approximately( transform.position.y, _nextPosition.y ) )
                {
                    anim.Play( PlayerAnimations[ 0 ] );
                    _movementState = State.Nothing;
                }

                break;
        }

        if ( xMove == 0 && yMove == 0 )
        {
            idleTimer += Time.deltaTime;
            if ( idleTimer >= 3 )
            {
                if ( !anim.GetCurrentAnimatorStateInfo( 0 ).IsName( PlayerAnimations[ 9 ] ) )
                {
                    anim.Play( PlayerAnimations[ 9 ] );
                }
                if(DamageTimer >= 1)
                {
                    _points -= 3;
                    PlayerEvents.PointsChangedEvent.Invoke( -3 );
                    DamageTimer = 0;
                }
                DamageTimer += Time.deltaTime;
            }
        } else
        {
            idleTimer = 0;
        }
    }
    public float TilesToMove;

    /// <summary>
    /// Makes the player stand still when doing nothing,
    /// Slide to the next point using <see cref="_walkingSpeed"/> if they are sliding,
    /// Run in free movement using <see cref="_runningSpeed"/> if they are running.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <author>Aven Presseisen</author>
    void MovePlayer()
    {
        transform.position = _movementState switch
        {
            State.Nothing => transform.position,
            State.Sliding => Vector2.MoveTowards( transform.position, _nextPosition, _walkingSpeed * Time.deltaTime ),
            _ => throw new ArgumentOutOfRangeException( nameof( _movementState ) ),
        };

    }

    //increases _points and calls the event, put it in a separate function so i wouldn't have to copy paste both lines
    void IncreasePoints()
    {
        _points += _pointIncrease;
        PlayerEvents.PointsChangedEvent.Invoke( _pointIncrease );
    }
    void DecreasePoints()
    {
        if ( _points > 0 )
        {
            _points += DamageTaken;
            PlayerEvents.PointsChangedEvent.Invoke( DamageTaken );
        }
    }


    void RandomClip()
    {
        Debug.Log( "play" );
        int index = UnityEngine.Random.Range( 0, HurtClips.Length );
        AudioSource.clip = HurtClips[index];
        AudioSource.Play();

    }

    void BeatWindowStart() //sets the beat to be active at the start of the window
    {
        beatActive = true;
    }

    void BeatWindowEnd() //sets the beat to be inactive after the window, in case it wasn't already
    {
        beatActive = false;
    }


    void OnTriggerEnter2D( Collider2D collision )
    {
        if ( collision.gameObject.CompareTag( "Bullet" ) )
        {
            Debug.Log( "Hit" );
            RandomClip();


            StartCoroutine( ChangeMaterial() );

            DecreasePoints();
        }
    }


    public IEnumerator ChangeMaterial()
    {

        gameObject.GetComponent< SpriteRenderer >().material = DamageMaterial;
        yield return new WaitForSeconds( 0.3f );


        gameObject.GetComponent< SpriteRenderer >().material = OriginalMaterial;
    }

    void DazzlingDance()
    {
        Debug.Log( "Dazzling Dance" );
        GameObject[] bullets = GameObject.FindGameObjectsWithTag( "Bullet" );
        _points += DAZZPOINTS;
        PlayerEvents.PointsChangedEvent.Invoke( DAZZPOINTS );
        DazzlingDancePoints -= DAZZPOINTS;
        foreach ( GameObject bullet in bullets )
        {
            _points += DAZZPOINTS;
            PlayerEvents.PointsChangedEvent.Invoke( DAZZPOINTS );
            
            GameObject deathParts = Instantiate( DeathParticles, bullet.transform.position, Quaternion.identity );
            Destroy( deathParts, 1.0f );
            DazzlingDancePoints -= DAZZPOINTS;
            Destroy( bullet );
        }

        PlayerEvents.PointsChangedEvent.Invoke( -75 );
    }

    void OnPointsChanged( int value )
    {
        if ( DazzlingDancePoints <= 100 )
        {
            DazzlingDancePoints += value;
        }

    }
    void PlayAnimation()
    {
        if ( xMove == 0 && yMove > 0 )
        {
            anim.Play( PlayerAnimations[ 1 ] );
        }

        if ( xMove > 0 && yMove > 0 )
        {
            anim.Play( PlayerAnimations[ 2 ] );
        }

        if ( xMove > 0 && yMove == 0 )
        {
            anim.Play( PlayerAnimations[ 3 ] );
        }

        if ( xMove > 0 && yMove < 0 )
        {
            anim.Play( PlayerAnimations[ 4 ] );
        }

        if ( xMove == 0 && yMove < 0 )
        {
            anim.Play( PlayerAnimations[ 5 ] );
        }

        if ( xMove < 0 && yMove < 0 )
        {
            anim.Play( PlayerAnimations[ 6 ] );
        }

        if ( xMove < 0 && yMove == 0 )
        {
            anim.Play( PlayerAnimations[ 7 ] );
        }

        if ( xMove < 0 && yMove > 0 )
        {
            anim.Play( PlayerAnimations[ 8 ] );
        }
    }

}
