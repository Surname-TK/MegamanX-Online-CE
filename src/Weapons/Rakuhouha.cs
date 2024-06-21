﻿using System;
using SFML.Graphics;

namespace MMXOnline;

public enum RakuhouhaType {
	Rakuhouha,
	CFlasher,
	Rekkoha,
	ShinMessenkou,
	DarkHold,
}

public class RakuhouhaWeapon : Weapon {
	public static RakuhouhaWeapon netWeapon = new();
	
	public RakuhouhaWeapon() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		rateOfFire = 1;
		index = (int)WeaponIds.Rakuhouha;
		weaponBarBaseIndex = 27;
		weaponBarIndex = 33;
		killFeedIndex = 16;
		weaponSlotIndex = 51;
		type = (int)RakuhouhaType.Rakuhouha;
		displayName = "Rakuhouha";
		description = new string[] { "Channels stored energy in one blast.", "Energy cost: 16" };
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 16;
	}

	public static Weapon getWeaponFromIndex(Player player, int index) {
		return index switch {
			(int)RakuhouhaType.Rakuhouha => new RakuhouhaWeapon(),
			(int)RakuhouhaType.CFlasher => new CFlasher(),
			(int)RakuhouhaType.Rekkoha => new RekkohaWeapon(),
			_ => throw new Exception("Invalid Zero hyouretsuzan weapon index!")
		};
	}
}

public class RekkohaWeapon : Weapon {
	public static RekkohaWeapon netWeapon = new();

	public RekkohaWeapon() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		rateOfFire = 2;
		index = (int)WeaponIds.Rekkoha;
		weaponBarBaseIndex = 40;
		weaponBarIndex = 34;
		killFeedIndex = 38;
		weaponSlotIndex = 63;
		type = (int)RakuhouhaType.Rekkoha;
		displayName = "Rekkoha";
		description = new string[] { "Summon down pillars of light energy.", "Energy cost: 28" };
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 28;
	}
}

public class CFlasher : Weapon {
	public static CFlasher netWeapon = new();
	
	public CFlasher() : base() {
		//damager = new Damager(player, 2, 0, 0.5f);
		ammo = 0;
		rateOfFire = 1f;
		index = (int)WeaponIds.CFlasher;
		weaponBarBaseIndex = 41;
		weaponBarIndex = 35;
		killFeedIndex = 81;
		weaponSlotIndex = 64;
		type = (int)RakuhouhaType.CFlasher;
		displayName = "Messenkou";
		description = new string[] { "A less damaging blast that can pierce enemies.", "Energy cost: 8" };
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 8;
	}
}

public class ShinMessenkou : Weapon {
	public ShinMessenkou() : base() {
		//damager = new Damager(player, 4, Global.defFlinch, 0.5f);
		ammo = 0;
		rateOfFire = 1f;
		index = (int)WeaponIds.ShinMessenkou;
		killFeedIndex = 86;
		type = (int)RakuhouhaType.ShinMessenkou;
		weaponBarBaseIndex = 43;
		weaponBarIndex = 37;
		weaponSlotIndex = 64;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 16;
	}
}

public class Rakuhouha : CharState {
	public Weapon weapon;
	Anim? rakuanim;
	RakuhouhaType type { get { return (RakuhouhaType)weapon.type; } }
	bool fired = false;
	bool fired2 = false;
	bool fired3 = false;
	const float shinMessenkouWidth = 40;
	public DarkHoldProj? darkHoldProj;
	public Rakuhouha(
		Weapon weapon
	) : base(
		(weapon.type == (int)RakuhouhaType.DarkHold) ? "darkhold" : 
		weapon.type == (int)RakuhouhaType.CFlasher ||
		weapon.type == (int)RakuhouhaType.DarkHold ? "cflasher" : "rakuhouha"
	) {
		this.weapon = weapon;
		invincible = true;
	}

	public override void update() {
		base.update();
		bool isCFlasher = type == RakuhouhaType.CFlasher;
		bool isRakuhouha = type == RakuhouhaType.Rakuhouha;
		bool isShinMessenkou = type == RakuhouhaType.ShinMessenkou;
		bool isDarkHold = type == RakuhouhaType.DarkHold;
		// isDarkHold = true;
		if (character.frameIndex == 5 && !once && !isDarkHold) {
			once = true;
			rakuanim = new Anim(
				character.pos.addxy(character.xDir, 0),
				"zero_rakuanim", character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);
		}
		float x = character.pos.x;
		float y = character.pos.y;
		if (character.frameIndex > 7 && !fired) {
			fired = true;

			if (isShinMessenkou) {
				character.playSound("zeroshinmessenkoubullet", forcePlay: false, sendRpc: true);
				new ShinMessenkouProj(weapon, new Point(x - shinMessenkouWidth, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
				new ShinMessenkouProj(weapon, new Point(x + shinMessenkouWidth, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
			} else if (isDarkHold) {
				invincible = false;
				darkHoldProj = new DarkHoldProj(
					weapon, new Point(x, y - 20), character.xDir, player, player.getNextActorNetId(), rpc: true
				);
			} else {
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -1, 0, player, player.getNextActorNetId(), 180, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -0.92f, -0.38f, player, player.getNextActorNetId(), 135, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -0.7f, -0.7f, player, player.getNextActorNetId(), 135, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, -0.38f, -0.92f, player, player.getNextActorNetId(), 135, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0, -1, player, player.getNextActorNetId(), 90, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0.92f, -0.38f, player, player.getNextActorNetId(), 45, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0.71f, -0.71f, player, player.getNextActorNetId(), 45, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 0.38f, -0.92f, player, player.getNextActorNetId(), 45, rpc: true);
				new RakuhouhaProj(weapon, new Point(x, y), isCFlasher, 1, 0, player, player.getNextActorNetId(), 0, rpc: true);
			}

			if (!isCFlasher && !isDarkHold) {
				character.shakeCamera(sendRpc: true);
				character.playSound("rakuhouha", sendRpc: true);
			} else if (isCFlasher && !isDarkHold) {
				character.playSound("cflasher", sendRpc: true);
			} else if (!isCFlasher && isDarkHold) {
				character.playSound("darkhold", forcePlay: false, sendRpc: true);
				if (Helpers.randomRange(0, 1) == 0) {
					character.playSound("znnigerunayo", forcePlay: false, sendRpc: true);
				} else {
					character.playSound("znowarida", forcePlay: false, sendRpc: true);
				}
			}
		}

		if (!fired2 && isShinMessenkou && character.frameIndex > 11) {
			fired2 = true;
			character.playSound("zeroshinmessenkoubullet", forcePlay: false, sendRpc: true);
			new ShinMessenkouProj(weapon, new Point(x - shinMessenkouWidth * 2, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
			new ShinMessenkouProj(weapon, new Point(x + shinMessenkouWidth * 2, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
		}

		if (!fired3 && isShinMessenkou && character.frameIndex > 14) {
			fired3 = true;
			character.playSound("zeroshinmessenkoubullet", forcePlay: false, sendRpc: true);
			new ShinMessenkouProj(weapon, new Point(x - shinMessenkouWidth * 3, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
			new ShinMessenkouProj(weapon, new Point(x + shinMessenkouWidth * 3, y), character.xDir, player, player.getNextActorNetId(), rpc: true);
		}

		if (character.isAnimOver()) {
			character.changeState(new Idle());
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(CharState newState) {
		weapon.shootTime = weapon.rateOfFire;
		base.onExit(newState);
	}
}

public class RakuhouhaProj : Projectile {
	bool isCFlasher;
	public RakuhouhaProj(
		Weapon weapon, Point pos, bool isCFlasher, float xVel,
		float yVel, Player player, ushort netProjId, int angle, bool rpc = false
	) : base(
		weapon, pos, xVel >= 0 ? 1 : -1, 300, 4, player, isCFlasher ? "cflasher" : "rakuhouha",
		Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer
	) {
		this.isCFlasher = isCFlasher;

		if (angle == 45) {
			var sprite = isCFlasher ? "cflasher_diag" : "rakuhouha_diag";
			changeSprite(sprite, false);
		} else if (angle == 90) {
			var sprite = isCFlasher ? "cflasher_up" : "rakuhouha_up";
			changeSprite(sprite, false);
		} else if (angle == 135) {
			xDir = -1;
			var sprite = isCFlasher ? "cflasher_diag" : "rakuhouha_diag";
			changeSprite(sprite, false);
		} else if (angle == 180) {
			xDir = -1;
		}

		if (!isCFlasher) {
			fadeSprite = "rakuhouha_fade";
			damager.damage = 3;
		} else {
			damager.damage = 2;
			damager.hitCooldown = 0.5f;
			damager.flinch = 0;
			destroyOnHit = false;
		}

		reflectable = true;
		projId = (int)ProjIds.Rakuhouha;
		if (isCFlasher) projId = (int)ProjIds.CFlasher;
		vel.x = xVel * 300;
		vel.y = yVel * 300;

		if (rpc) {
			rpcCreate(
				pos, player, netProjId, xDir,
				new Byte[]{
					(byte)angle,
					(byte)MathInt.Round(xVel * 100),
					(byte)MathInt.Round(yVel * 100)
				}
			);
		}
	}

	public override void update() {
		base.update();
		if (time > 0.5) {
			destroySelf(fadeSprite);
		}
	}
}

public class ShinMessenkouProj : Projectile {
	int state = 0;
	public ShinMessenkouProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 4, player, "shinmessenkou_start", Global.defFlinch, 1f, netProjId, player.ownedByLocalPlayer) {
		maxTime = 0.6f;
		destroyOnHit = false;
		projId = (int)ProjIds.ShinMessenkou;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (state == 0 && isAnimOver()) {
			state = 1;
			changeSprite("shinmessenkou_proj", true);
			vel.y = -300;
		}
	}
}

public class Rekkoha : CharState {
	bool fired1 = false;
	bool fired2 = false;
	bool fired3 = false;
	bool fired4 = false;
	bool fired5 = false;
	bool sound;
	int loop;
	public RekkohaEffect? effect;
	public Weapon weapon;
	public Rekkoha(Weapon weapon) : base("rekkoha", "", "", "") {
		this.weapon = weapon;
		invincible = true;
	}

	public override void update() {
		base.update();

		float topScreenY = Global.level.getTopScreenY(character.pos.y);

		if (character.frameIndex == 13 && loop < 15) {
			character.frameIndex = 10;
			loop++;
		}

		if (character.frameIndex == 5 && !sound) {
			sound = true;
			character.playSound("rekkohax6", sendRpc: true);
		}

		if (stateTime > 26/60f && !fired1) {
			fired1 = true;
			new RekkohaProj(weapon, new Point(character.pos.x, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 0.6f && !fired2) {
			fired2 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 35, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 0.8f && !fired3) {
			fired3 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 70, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 1f && !fired4) {
			fired4 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 110, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}
		if (stateTime > 1.2f && !fired5) {
			fired5 = true;
			new RekkohaProj(weapon, new Point(character.pos.x - 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
			new RekkohaProj(weapon, new Point(character.pos.x + 150, topScreenY), player, player.getNextActorNetId(), rpc: true);
		}

		if (character.isAnimOver()) {
			character.changeState(new Idle());
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.isMainPlayer) {
			effect = new RekkohaEffect();
		}
	}

	public override void onExit(CharState newState) {
		weapon.shootTime = weapon.rateOfFire;
		base.onExit(newState);
	}
}

public class RekkohaProj : Projectile {
	float len = 0;
	private float reverseLen;
	private bool updatedDamager = false;

	public RekkohaProj(
		Weapon weapon, Point pos, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, 1, 0, 3, player, "rekkoha_proj",
		Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Rekkoha;
		vel.y = 400;
		maxTime = 1.6f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		netcodeOverride = NetcodeModel.FavorDefender;
	}

	public override void update() {
		base.update();
		len += Global.spf * 300;
		if (time >= 1f && !updatedDamager) {
			updateLocalDamager(0, 0);
			updatedDamager = true;
		}
		if (len >= 200) {
			len = 200;
			reverseLen += Global.spf * 200 * 2;
			if (reverseLen > 200) {
				reverseLen = 200;
			}
			vel.y = 100;
		}
		Rect newRect = new Rect(0, 0, 16, 60 + len - reverseLen);
		globalCollider = new Collider(newRect.getPoints(), true, this, false, false, 0, new Point(0, 0));
	}

	public override void render(float x, float y) {
		float finalLen = len - reverseLen;

		float newAlpha = 1;
		if (len <= 50) {
			newAlpha = len / 50;
		}
		if (reverseLen >= 100) {
			newAlpha = (200 - reverseLen) / 100;
		}

		alpha = newAlpha;

		float basePosY = System.MathF.Floor(pos.y + y);

		Global.sprites["rekkoha_proj_mid"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY - 63f,
			1, 1, null, alpha,
			1f, finalLen / 23f + 0.01f, zIndex
		);
		Global.sprites["rekkoha_proj_top"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY - 63f - finalLen,
			1, 1, null, alpha,
			1f, 1f, zIndex
		);
		Global.sprites["rekkoha_proj"].draw(
			sprite.frameIndex,
			pos.x + x,
			basePosY,
			1, 1, null, alpha,
			1f, 1f, zIndex
		);
	}
}


public class RekkohaEffect : Effect {
	public RekkohaEffect() : base(new Point(Global.level.camX, Global.level.camY)) {
	}

	public override void update() {
		base.update();
		pos.x = Global.level.camX;
		pos.y = Global.level.camY;

		if (effectTime > 2) {
			destroySelf();
		}
	}

	public override void render(float offsetX, float offsetY) {
		float scale = 1;
		if (Global.level.server.fixedCamera) {
			scale = 2;
		}

		offsetX += pos.x;
		offsetY += pos.y;

		float alpha = 0.5f;
		if (effectTime < 0.25f) {
			alpha = 0.5f * (effectTime * 4);
		}
		if (effectTime > 1.75f) {
			alpha = 0.5f - 0.5f * ((effectTime - 1.75f) * 4);
		}
		alpha *= 1.5f;

		for (int i = 0; i < 50 * scale; i++) {
			float offY = (effectTime * 448) * (i % 2 == 0 ? 1 : -1);
			while (offY > 596) offY -= 596;
			while (offY < -596) offY += 596;

			int index = i + (int)(effectTime * 20);

			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY - 596,
				1, 1, null, alpha, 1, 1, ZIndex.Background + 5
			);
			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY,
				1, 1, null, alpha, 1, 1, ZIndex.Background + 5
			);
			Global.sprites["rekkoha_effect_strip"].draw(
				index % 3,
				offsetX + i * 8, offsetY + offY + 596,
				1, 1, null, alpha, 1, 1, ZIndex.Background + 5
			);
		}
	}
}

public class DarkHoldWeapon : Weapon {
	public DarkHoldWeapon() : base() {
		ammo = 0;
		rateOfFire = 2f;
		index = (int)WeaponIds.DarkHold;
		type = (int)RakuhouhaType.DarkHold;
		killFeedIndex = 175;
		weaponBarBaseIndex = 69;
		weaponBarIndex = 58;
		weaponSlotIndex = 122;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 16;
	}
}

public class DarkHoldProj : Projectile {
	public float radius = 10;
	public float attackRadius => (radius + 15);
	public ShaderWrapper? screenShader;
	public DarkHoldProj(
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 0, player, "empty", 0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1.25f;
		vel = new Point();
		projId = (int)ProjIds.DarkHold;
		setIndestructableProperties();
		Global.level.darkHoldProjs.Add(this);
		if (Options.main.enablePostProcessing) {
			screenShader = player.darkHoldScreenShader;
			updateShader();
		}
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		updateShader();

		if (time <= 0.5) {
			foreach (var gameObject in Global.level.getGameObjectArray()) {
				if (gameObject is Actor actor &&
					actor.ownedByLocalPlayer &&
					gameObject is IDamagable damagable &&
					damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null) &&
					inRange(actor)
				) {
					damager.applyDamage(damagable, false, weapon, this, projId);
				}
			}
		}
		if (time <= 0.5) {
			radius += Global.spf * 400;
		}
		if (time >= 1 && radius > 0) {
			radius -= Global.spf * 800;
			if (radius <= 0) {
				radius = 0;
			}
		}
	}

	public bool inRange(Actor actor) {
		return (actor.getCenterPos().distanceTo(pos) <= attackRadius);
	}

	public void updateShader() {
		if (screenShader != null) {
			var screenCoords = new Point(
				pos.x - Global.level.camX,
				pos.y - Global.level.camY
			);
			var normalizedCoords = new Point(
				screenCoords.x / Global.viewScreenW,
				1 - screenCoords.y / Global.viewScreenH
			);
			float ratio = Global.screenW / (float)Global.screenH;
			float normalizedRadius = (radius / Global.screenH);

			screenShader.SetUniform("ratio", ratio);
			screenShader.SetUniform("x", normalizedCoords.x);
			screenShader.SetUniform("y", normalizedCoords.y);
			if (Global.viewSize == 2) {
				screenShader.SetUniform("r", normalizedRadius * 0.5f);
			} else {
				screenShader.SetUniform("r", normalizedRadius);
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (screenShader == null) {
			var col = new Color(255, 251, 239, (byte)(164 - 164 * (time / maxTime)));
			var col2 = new Color(255, 219, 74, (byte)(224 - 224 * (time / maxTime)));
			DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, true, col, 1, zIndex + 1, true);
			DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, false, col2, 3, zIndex + 1, true, col2);
		}

	}

	public override void onDestroy() {
		base.onDestroy();
		Global.level.darkHoldProjs.Remove(this);
	}
}

public class DarkHoldState : CharState {
	public float stunTime = totalStunTime;
	public const float totalStunTime = 5;
	int frameIndex;
	public bool shouldDrawAxlArm = true;
	public float lastArmAngle = 0;
	public DarkHoldState(Character character) : base(character?.sprite?.name ?? "grabbed") {
		immuneToWind = true;

		this.frameIndex = character?.frameIndex ?? 0;
		if (character is Axl axl) {
			this.shouldDrawAxlArm = axl.shouldDrawArm();
			this.lastArmAngle = axl.netArmAngle;
		}
	}

	public override void update() {
		base.update();
		character.stopMoving();
		stunTime -= player.mashValue();
		if (stunTime <= 0) {
			stunTime = 0;
			character.changeToIdleOrFall();
		}
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character)) return false;
		if (character.darkHoldInvulnTime > 0) return false;
		if (character.isInvulnerable()) return false;
		if (character.isVaccinated()) return false;
		return !character.isCCImmune() && !character.charState.invincible;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.frameSpeed = 0;
		character.frameIndex = frameIndex;
		character.stopMoving();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
		character.frameSpeed = 1;
		character.darkHoldInvulnTime = 1;
	}
}
