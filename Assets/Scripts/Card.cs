using System;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using Save;
using UnityEngine;
using Random = UnityEngine.Random;

public class Card : MonoBehaviour
{
    public Draggable draggable;
    
    [SerializeField] private List<SpriteRenderer> points;
    [SerializeField] private Sprite starSprite, bombSprite;
    [SerializeField] private Transform visualContents;
    [SerializeField] private GameObject rotateIcon;
    [SerializeField] private List<GameObject> directionIndicators;

    private List<Pip> pips;
    private CardData data;
    private List<Pip> shakingPips;

    public bool IsRotator => data.IsRotator;
    public bool RotatesClockwise => data.RotatesClockwise;

    public void Setup(CardData cardData)
    {
        shakingPips = new List<Pip>();
        data = cardData.Clone();

        if (data.IsRotator)
        {
            rotateIcon.SetActive(true);
            data.directions.ForEach(d => directionIndicators[d].SetActive(true));
        }
        
        if (data.type == CardType.RotateLeft)
        {
            rotateIcon.transform.Mirror();
        }

        PositionPips();
    }

    private void Update()
    {
        if (shakingPips.Count > 0)
        {
            shakingPips.ForEach(p => p.Shake());
        }
    }

    public void ShakePip(Pip pip)
    {
        shakingPips.Add(pip);
    }

    private void PositionPips()
    {
        points.ForEach(p => p.gameObject.SetActive(false));
        
        pips = points.Where((_, i) => data.pips.Contains(i)).Select(p =>
        {
            var i = points.IndexOf(p);
            p.gameObject.SetActive(true);

            var isStar = data.stars.Contains(i);
            if (isStar)
            {
                p.sprite = starSprite;
                p.transform.Rotate(0, 0, Random.Range(0f, 360f));
            }

            var isBomb = !isStar && data.bombs.Contains(i);
            if (isBomb)
            {
                p.sprite = bombSprite;
                p.transform.Rotate(0, 0, Random.Range(-40f, 40f));
            }

            return new Pip(this, p, i % 3, i / 3, isStar, isBomb);
        }).ToList();
    }

    public void Rotate(bool clockWise)
    {
        var dir = clockWise ? 1 : -1;
        VisualRotate(dir);
        data.Rotate(dir);
        PositionPips();
    }

    private void VisualRotate(int dir)
    {
        if (pips.Any())
        {
            visualContents.rotation = Quaternion.Euler(0, 0, 90f * dir);
            Tweener.RotateToBounceOut(visualContents, Quaternion.identity, 0.3f);
            return;
        }

        var angle = visualContents.rotation.eulerAngles.z - dir * 90f;
        Tweener.RotateToBounceOut(visualContents, Quaternion.Euler(0, 0, angle), 0.3f);
    }

    public IEnumerable<Pip> GetPoints(bool addBase = false)
    {
        if (addBase)
        {
            var pos = GetBasePosition();
            pips.ForEach(p => p.AddBase(pos));    
        }
        
        return pips;
    }

    private IntPair GetBasePosition()
    {
        var p = transform.position;
        return GetBasePositionFor(p);
    }

    public IntPair GetCoordinates()
    {
        var p = transform.position;
        var coords = GetBasePositionFor(p);
        return new IntPair(coords.x / 3, coords.y / 3);
    }

    public IntPair GetBasePositionFor(Vector3 p)
    {
        return new IntPair(((int)p.x + 2) * 3, (-(int)p.y + 2) * 3);
    }

    public IEnumerable<Transform> GetAllPips()
    {
        return points.Select(p => p.transform);
    }

    public IEnumerable<Vector3> GetDirections()
    {
        var q = visualContents.rotation;
        var baseDirections = new List<Vector3> { Vector3.up, Vector3.right, Vector3.down, Vector3.left };
        return data.directions.Select(i => q * baseDirections[i]);
    }
}

public class IntPair
{
    public int x, y;

    public IntPair(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class Pip
{
    public SpriteRenderer sprite;
    public int x, y;
    public bool isStar, isBomb;
    public bool isShaking;

    private Vector3 origin;
    private Card card;

    public Pip(Card card, SpriteRenderer sprite, int x, int y, bool isStar, bool isBomb)
    {
        this.sprite = sprite;
        this.x = x;
        this.y = y;
        this.isStar = isStar;
        this.isBomb = isBomb;
        this.card = card;
    }

    public float GetDistanceTo(Pip pip)
    {
        return Mathf.Pow(pip.x - x, 2) + Mathf.Pow(pip.y - y, 2);
    }
    
    public void AddBase(IntPair offset)
    {
        x += offset.x;
        y += offset.y;
    }

    public void StartShaking()
    {
        origin = sprite.transform.position;
        card.ShakePip(this);
    }

    public void Shake()
    {
        sprite.transform.position = origin.RandomOffset(0.02f);
    }

    public Card GetCard()
    {
        return card;
    }
}