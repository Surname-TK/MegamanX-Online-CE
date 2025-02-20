namespace MMXOnline;

public class RepliforceGeneral : Maverick
{
	public Weapon meleeWeapon;
	public RepliforceGeneral(Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
		base(player, pos, destPos, xDir, netId, ownedByLocalPlayer)
	{
		stateCooldowns.Add(typeof(MShoot), new MaverickStateCooldown(false, true, 0.75f));
		stateCooldowns.Add(typeof(RepliGenArmState), new MaverickStateCooldown(false, true, 0.75f));

		weapon = new Weapon(WeaponIds.RepliGenGeneric, 0);
		meleeWeapon = new Weapon(WeaponIds.RepliGenGeneric, 0);

		armorClass = ArmorClass.Heavy;
		canStomp = true;
		canFly = true;
		flyBarIndexes = (44, 38);
		maxFlyBar = 960;
		flyBar = 960;

		spriteToCollider["head*"] = getGeneralHeadCollider();
		spriteToCollider["body*"] = getGeneralBodyCollider();

		awardWeaponId = WeaponIds.Buster;
		weakWeaponId = WeaponIds.TwinSlasher;
		weakMaverickWeaponId = WeaponIds.TwinSlasher;

		netActorCreateId = NetActorCreateId.RepliforceGeneral;
		netOwner = player;
		if (sendRpc)
		{
			createActorRpc(player.id);
		}
	}
	public Collider getGeneralHeadCollider() {
		var rect = new Rect(0, 0, 24, 24);
		return new Collider(rect.getPoints(), true, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}
	public Collider getGeneralBodyCollider() {
		var rect = new Rect(0, 0, 16, 108);
		return new Collider(rect.getPoints(), true, this, false, false, HitboxFlag.None, new Point(0, 0));
	}

	public override void update()
	{
		base.update();
		if (aiBehavior == MaverickAIBehavior.Control)
		{
			if (state is MIdle || state is MRun)
			{
				if (input.isPressed(Control.Shoot, player))
				{
					changeState(getShootState(false));
				}
				else if (input.isPressed(Control.Special1, player))
				{
					changeState(new RepliGenArmState());
				}
				else if (input.isPressed(Control.Dash, player))
				{
					changeState(new RepliGenStompState());
				}
			}
			else if (state is MJump || state is MFall)
			{
			}
		}
	}

	public override string getMaverickPrefix()
	{
		return "general";
	}
	public override Point getCenterPos(){
		return pos.addxy(0, -60);
	}
	
	public override Projectile? getProjFromHitbox(Collider collider, Point centerPoint) {
		if (collider.name == "stomp" && collider.isAttack()) {
			return new GenericMeleeProj(
				new Weapon(), centerPoint, ProjIds.RepliGenMelee, player,
				damage: 4, flinch: Global.defFlinch, hitCooldown: 1f
			);
		} else if (collider.name == "body") {
			return new GenericMeleeProj(
				new Weapon(), centerPoint, ProjIds.RepliGenBody, player,
				damage: 0, flinch: 0, hitCooldown: 1, isShield: true
			);
		}
		return null;
	}

	public override MaverickState getRandomAttackState()
	{
		return aiAttackStates().GetRandomItem();
	}

	public override MaverickState[] aiAttackStates()
	{
		return new MaverickState[]
		{
			new RepliGenShootState(),
			new RepliGenStompState(),
			new RepliGenArmState(),
		};
	}

	public MaverickState getShootState(bool isAI)
	{
		var mshoot = new MShoot((Point pos, int xDir) =>
		{
			playSound("???", sendRpc: true);
			new RepliGenBeltProj(weapon, pos, xDir, player, player.getNextActorNetId(), sendRpc: true);
		}, null);
		if (isAI)
		{
			mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.75f);
		}
		return mshoot;
	}
}

public class RepliGenBeltProj : Projectile
{
	public RepliGenBeltProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false) :
		base(weapon, pos, xDir, 250, 3, player, "maverickabbr_proj", 0, 0.01f, netProjId, player.ownedByLocalPlayer)
	{
		projId = (int)ProjIds.RepliGenBelt;
		maxTime = 2.75f;

		if (sendRpc)
		{
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update()
	{
		base.update();
	}
}

public class RepliGenShootState : MaverickState
{
	bool shotOnce;
	public RepliGenShootState() : base("shoot", "")
	{
		exitOnAnimEnd = true;
	}

	public override void update()
	{
		base.update();

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null)
		{
			shotOnce = true;
			maverick.playSound("???", sendRpc: true);
			new RepliGenBeltProj(maverick.weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), sendRpc: true);
		}
	}
}

public class RepliGenStompState : MaverickState
{
	public RepliGenStompState() : base("melee", "")
	{
		exitOnAnimEnd = true;
	}

	public override void update()
	{
		base.update();
	}
}

public class RepliGenArmState : MaverickState
{
	public RepliGenArmState() : base("special", "")
	{
		exitOnAnimEnd = true;
	}

	public override void update()
	{
		base.update();
	}
}
