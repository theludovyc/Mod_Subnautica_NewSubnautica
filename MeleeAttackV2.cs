using System;
using UnityEngine;
using UWE;

namespace NewSubnautica
{
	public class MeleeAttackV2 : MonoBehaviour, IManagedUpdateBehaviour, IManagedBehaviour
	{
		static int biteAnimID = Animator.StringToHash("bite");

		public virtual void OnTouch(Collider collider)
		{
			if (!base.enabled)
			{
				return;
			}

			if (this.frozen)
			{
				return;
			}

			if (this.creature.Aggression.Value < this.biteAggressionThreshold)
			{
				return;
			}

			if (Time.time < this.timeLastBite + this.biteInterval)
			{
				return;
			}

			GameObject target = this.GetTarget(collider);

			if (this.ignoreSameKind && global::Utils.CompareTechType(base.gameObject, target))
			{
				return;
			}

			if (this.CanBite(target))
			{
				this.timeLastBite = Time.time;

				LiveMixin component2 = target.GetComponent<LiveMixin>();

				if (component2 != null && component2.IsAlive())
				{
					component2.TakeDamage(this.GetBiteDamage(target), default(Vector3), DamageType.Normal, null);
					component2.NotifyCreatureDeathsOfCreatureAttack();
				}

				Vector3 position = collider.ClosestPointOnBounds(this.mouth.transform.position);

				if (this.damageFX != null)
				{
					UnityEngine.Object.Instantiate<GameObject>(this.damageFX, position, this.damageFX.transform.rotation);
				}

				if (this.attackSound != null)
				{
					global::Utils.PlayEnvSound(this.attackSound, position, 20f);
				}

				this.creature.Aggression.Add(-this.biteAggressionDecrement);

				if (component2 != null && !component2.IsAlive())
				{
					this.TryEat(component2.gameObject, false);
				}

				base.gameObject.SendMessage("OnMeleeAttack", target, SendMessageOptions.DontRequireReceiver);
			}
		}

		public virtual bool CanBite(GameObject target)
		{
			Player component = target.GetComponent<Player>();

			if (component != null && !this.canBitePlayer && !component.CanBeAttacked())
			{
				return false;
			}

			if ((!this.canBiteCreature || target.GetComponent<Creature>() == null) && (!this.canBiteVehicle || target.GetComponent<Vehicle>() == null) && (!this.canBiteCyclops || target.GetComponent<CyclopsDecoy>() == null))
			{
				return false;
			}

			Vector3 direction = target.transform.position - base.transform.position;

			float magnitude = direction.magnitude;

			int num = UWE.Utils.RaycastIntoSharedBuffer(base.transform.position, direction, magnitude, -5, QueryTriggerInteraction.Ignore);

			for (int i = 0; i < num; i++)
			{
				Collider collider = UWE.Utils.sharedHitBuffer[i].collider;

				GameObject gameObject = (collider.attachedRigidbody != null) ? collider.attachedRigidbody.gameObject : collider.gameObject;

				if (!(gameObject == target) && !(gameObject == base.gameObject) && !(gameObject.GetComponent<Creature>() != null))
				{
					return false;
				}
			}

			return true;
		}

		public string GetProfileTag()
		{
			return "MeleeAttack";
		}

		public int managedUpdateIndex { get; set; }

		private void OnEnable()
		{
			BehaviourUpdateUtils.Register(this);
		}

		protected virtual void OnDisable()
		{
			BehaviourUpdateUtils.Deregister(this);
		}

		private void OnDestroy()
		{
			BehaviourUpdateUtils.Deregister(this);
		}

		public virtual bool CanEat(BehaviourType behaviourType, bool holdingByPlayer = false)
		{
			BehaviourType behaviourType2 = BehaviourData.GetBehaviourType(base.gameObject);
			return behaviourType == BehaviourType.SmallFish || (behaviourType == BehaviourType.MediumFish && (holdingByPlayer || behaviourType2 == BehaviourType.Shark));
		}

		protected virtual float GetBiteDamage(GameObject target)
		{
			return this.biteDamage;
		}

		public GameObject GetTarget(Collider collider)
		{
			GameObject gameObject = collider.gameObject;
			if (gameObject.GetComponent<LiveMixin>() == null && collider.attachedRigidbody != null)
			{
				gameObject = collider.attachedRigidbody.gameObject;
			}
			return gameObject;
		}

		protected bool TryEat(GameObject preyGameObject, bool holdingByPlayer = false)
		{
			bool result = false;
			BehaviourType behaviourType = BehaviourData.GetBehaviourType(preyGameObject);
			if (this.CanEat(behaviourType, holdingByPlayer))
			{
				base.SendMessage("OnFishEat", preyGameObject, SendMessageOptions.DontRequireReceiver);
				float num = 1f;
				if (behaviourType == BehaviourType.MediumFish)
				{
					num = 1.5f;
				}
				if (behaviourType == BehaviourType.Shark)
				{
					num = 3f;
				}
				if (preyGameObject.GetComponent<Creature>() != null)
				{
					UnityEngine.Object.Destroy(preyGameObject);
				}
				this.creature.Hunger.Add(-this.eatHungerDecrement * num);
				this.creature.Happy.Add(this.eatHappyIncrement * num);
				Peeper component = preyGameObject.GetComponent<Peeper>();
				if (component != null && component.isHero)
				{
					InfectedMixin component2 = base.GetComponent<InfectedMixin>();
					if (component2 != null)
					{
						component2.Heal(0.5f);
					}
				}
				result = true;
			}
			return result;
		}

		public void ManagedUpdate()
		{
			bool flag = Time.time - this.timeLastBite < 0.2f;
			if (flag != this.wasBiting || !this.initBiting)
			{
				this.animator.SetBool(biteAnimID, flag);
			}
			this.wasBiting = flag;
			this.initBiting = true;
		}

		protected float timeLastBite;

		protected bool frozen;

		private bool wasBiting;

		private bool initBiting;

		public void OnFreeze()
		{
			this.frozen = true;
		}

		public void OnUnfreeze()
		{
			this.frozen = false;
		}

		public float biteAggressionThreshold = 0.3f;

		public float biteInterval = 1f;

		public float biteDamage = 30f;

		public float eatHungerDecrement = 0.5f;

		public float eatHappyIncrement = 0.5f;

		public float biteAggressionDecrement = 0.4f;

		public FMOD_StudioEventEmitter attackSound;

		[AssertNotNull]
		public GameObject mouth;

		[AssertNotNull]
		public LastTarget lastTarget;

		[AssertNotNull]
		public Creature creature;

		[AssertNotNull]
		public LiveMixin liveMixin;

		public GameObject damageFX;

		[AssertNotNull]
		public Animator animator;

		public bool ignoreSameKind;

		public bool canBiteCreature = true;

		public bool canBitePlayer = true;

		public bool canBiteVehicle;

		public bool canBiteCyclops;

		public void copyMeleeAttack(MeleeAttack ma)
		{
			biteAggressionThreshold = ma.biteAggressionThreshold;

			biteInterval = ma.biteInterval;

			biteDamage = ma.biteDamage;

			eatHungerDecrement = ma.eatHungerDecrement;

			eatHappyIncrement = ma.eatHappyIncrement;

			biteAggressionDecrement = ma.biteAggressionDecrement;

			attackSound = ma.attackSound;

			mouth = ma.mouth;

			lastTarget = ma.lastTarget;

			creature = ma.creature;

			liveMixin = ma.liveMixin;

			damageFX = ma.damageFX;

			animator = ma.animator;

			ignoreSameKind = ma.ignoreSameKind;

			canBiteCreature = ma.canBiteCreature;

			canBitePlayer = ma.canBitePlayer;

			canBiteVehicle = ma.canBiteVehicle;

			canBiteCyclops = ma.canBiteCyclops;
		}
	}
}