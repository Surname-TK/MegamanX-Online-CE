using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BubbleSplash : Weapon {
	public static BubbleSplash netWeapon = new();

	public List<BubbleSplashProj> bubblesOnField = new List<BubbleSplashProj>();
	public List<float> bubbleAfterlifeTimes = new List<float>();
	public float hyperChargeDelay;

	public BubbleSplash() : base() {
		shootSounds = new string[] { "bubbleSplash", "bubbleSplash", "bubbleSplash", "bubbleSplashCharged" };
		fireRate = 2;
		isStream = true;
		index = (int)WeaponIds.BubbleSplash;
		weaponBarBaseIndex = 10;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 10;
		killFeedIndex = 21;
		weaknessIndex = (int)WeaponIds.SpinWheel;
		//switchCooldown = 0.25f;
		switchCooldownFrames = 15;
		damage = "1/1";
		ammousage = 0.5;
		//effect = "Shoot a Stream up to 7 bubbles. C:Jump Boost.";
		effect = "Charged: Grants Jump Boost.";
		maxAmmo = 28;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) {
			return 4;
		}
		return 0.25f;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		if (hyperChargeDelay > 0) return false;

		if (bubblesOnField.Count < 7) {
			return bubblesOnField.Count + bubbleAfterlifeTimes.Count < 7;
		} else {
			return bubblesOnField.Count + bubbleAfterlifeTimes.Count < 7;
		}
	}

	public override void update() {
		base.update();
		Helpers.decrementFrames(ref hyperChargeDelay);

		for (int i = bubblesOnField.Count - 1; i >= 0; i--) {
			if (bubblesOnField[i].destroyed) {
				float timeCutShort = bubblesOnField[i].maxTime - bubblesOnField[i].time;
				bubbleAfterlifeTimes.Add(0.4f);
				bubblesOnField.RemoveAt(i);
				continue;
			}
		}

		for (int i = bubbleAfterlifeTimes.Count - 1; i >= 0; i--) {
			bubbleAfterlifeTimes[i] -= Global.spf;
			if (bubbleAfterlifeTimes[i] <= 0) {
				bubbleAfterlifeTimes.RemoveAt(i);
			}
		}
	}

	// Friendly reminder that this method MUST be deterministic across all clients, i.e. don't vary it on a field that could vary locally.
	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			if (player.ownedByLocalPlayer) {
				int type = 0;
				if (player.input.isHeld(Control.Up, player)) {
					type = 1;
				}
				var proj = new BubbleSplashProj(type, pos, xDir, player, player.getNextActorNetId(), rpc: true);
				bubblesOnField.Add(proj);
			}
		} else if (chargeLevel >= 3 && character is MegamanX mmx) {
			//player.setNextActorNetId(player.getNextActorNetId());
			mmx.popAllBubbles();
			for (int i = 0; i < 6; i++) {
				var bubble = new BubbleSplashProjCharged(
					this, pos, xDir, player, i, 
					player.getNextActorNetId(true), true);

				mmx?.chargedBubbles?.Add(bubble);

				if (i == 0) bubble.releasePlasma = player.hasPlasma();	
			}
		}
	}
}

public class BubbleSplashProj : Projectile {
	int size;
	float randY;
	float randT;
	int randBubble;

	public BubbleSplashProj(
		int type, Point pos, int xDir, Player player, ushort netProjId,
		int? size = null, int? randX = null, int? randY = null, float? randT = null,
		bool rpc = false
	) : base(
		BubbleSplash.netWeapon, pos, xDir,
		75, 0.5f, player, "bubblesplash_start", 0, 0f,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.BubbleSplash;
		destroyOnHit = false;

		// RNG shenanigans.
		if (randX == null) {
			randX = !(player.character.charState is Dash or AirDash) ? Helpers.randomRange(75, 125) : Helpers.randomRange(150, 250);
		}
		if (randY == null) {
			randY = Helpers.randomRange(75, 125);
		}
		if (size == null) {
			size = Helpers.randomRange(0, 2);
		}
		if (randT == null) {
			randT = Helpers.randomRange(75, 125);
		}

		// Create variables.
		this.size = size.Value;
		this.randT = (float)randT / 100f;
		this.randY = (float)randY;
		useGravity = false;
		maxTime = this.randT;
		vel.x *= randX.Value / 100f;
		vel.y = 0;

		randBubble = Helpers.randomRange(0, spriteVariants.Length - 1);
		switch (size) {
			case 0: fadeSprite = "bubblesplash_pop_small";
			break;
			case 1: fadeSprite = "bubblesplash_pop_medium";
			break;
			case 2: fadeSprite = "bubblesplash_pop_large";
			break;
		}
		fadeSound = "bubbleSplashPop";
		fadeOnAutoDestroy = true;


		if (rpc) {
			rpcCreate(
				pos, player, netProjId, xDir,
				(byte)type, (byte)size, (byte)randX, (byte)randY, (byte)randT
			);
		}
	}
	public override void onHitDamagable(IDamagable damagable){
		fadeOnAutoDestroy = false;
		if (sprite.name != fadeSprite || time > randT){
			time = 0;
			playSound(fadeSound, true, true);
			changeSprite(fadeSprite, true);
		}
	}
	public override void update() {
		base.update();
		if (sprite.name == "bubblesplash_start"){
			vel.y = 0;
		} else {
			if (vel.y == 0) { 
				vel.y = -20 * (randY / 100f);
			}
			if (isUnderwater()) {
				vel.y -= 6f;
			} else {
				vel.y -= 1.5f;
			}
		}
		if (sprite.name.Contains("proj") && !sprite.name.Contains("start")){
			switch (size) {
				case 0: if (frameIndex > 0) {frameSpeed = 0;}
				break;
				case 1: if (frameIndex > 1) {frameSpeed = 0;}
				break;
				case 2: if (frameIndex > 2) {frameSpeed = 0;}
				break;
			}
		} else if (sprite.name.Contains("start") && isAnimOver()) {
			changeSprite(spriteVariants[randBubble], true);
		} 

		if (sprite.name == fadeSprite){
			fadeOnAutoDestroy = false;
			vel = new Point(0, 0);
			if (isAnimOver()){
				destroySelfNoEffect();
			}
		}
	}

	public static string[] spriteVariants = {
		"bubblesplash_proj1",
		"bubblesplash_proj2",
		"bubblesplash_proj3",
	};

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BubbleSplashProj(
			arg.extraData[0], arg.pos, arg.xDir, arg.player, arg.netId,
			arg.extraData[1], arg.extraData[2], arg.extraData[3], arg.extraData[4]
		);
	}
}

public class BubbleSplashProjCharged : Projectile {
	public MegamanX character;
	public float yPos;
	public int size;
	public int randBubble;
	public BubbleSplashProjCharged(
		Weapon weapon, Point pos, int xDir, Player player, 
		int type, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 75, 1, player, "bubblesplash_start", 
		0, 0, netProjId, player.ownedByLocalPlayer
	) {
		useGravity = false;

		randBubble = Helpers.randomRange(0, spriteVariants.Length - 1);
		
		//if (size == null) {
			size = Helpers.randomRange(0, 2);
		//}

		switch (size) {
			case 0: fadeSprite = "bubblesplash_pop_small";
			break;
			case 1: fadeSprite = "bubblesplash_pop_medium";
			break;
			case 2: fadeSprite = "bubblesplash_pop_large";
			break;
		}

		character = (player.character as MegamanX);
		this.time = type * 0.2f;
		sprite.doesLoop = true;
		projId = (int)ProjIds.BubbleSplashCharged;

		isOwnerLinked = true;
		if (player.character != null) {
			owningActor = player.character;
		} 

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir, new byte[] { (byte)type });
		}

		canBeLocal = false;
	}
	
	public static string[] spriteVariants = {
		"bubblesplash_proj1",
		"bubblesplash_proj2",
		"bubblesplash_proj3",
	};

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BubbleSplashProjCharged(
			BubbleSplash.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		
		if (sprite.name.Contains("start") && isAnimOver()) {
			changeSprite(spriteVariants[randBubble], true);
		} else if (sprite.name.Contains("proj") && !sprite.name.Contains("start")){
			switch (size) {
				case 0: if (frameIndex > 0) {frameSpeed = 0;}
				break;
				case 1: if (frameIndex > 1) {frameSpeed = 0;}
				break;
				case 2: if (frameIndex > 2) {frameSpeed = 0;}
				break;
			}
		}

		if (character == null || !Global.level.gameObjects.Contains(character)  || (character.player.weapon is not BubbleSplash)) {
			destroySelf();
			return;
		}
		time += Global.spf;
		if (time > 2) time = 0;

		float x = 20 * MathF.Sin(time * 5);
		yPos = -15 * time;
		Point newPos = character.pos.addxy(x, yPos);
		changePos(newPos);
	}

	public override void onDestroy() {
		if (character != null) {
			character?.chargedBubbles?.Remove(this);
		}
	}
}
