using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class SoulBody : Weapon {

	public static SoulBody netWeapon = new();
	public SoulBody() : base() {
		index = (int)WeaponIds.SoulBody;
		fireRate = 90;
		weaponSlotIndex = 125;
        weaponBarBaseIndex = 74;
        weaponBarIndex = 63;
		shootSounds = new string[] {"","","",""};
		weaknessIndex = (int)WeaponIds.LightningWeb;
		hasCustomAnim = true;
		/* damage = "1/3";
		hitcooldown = "0.5/0.75";
		Flinch = "0/13";
		FlinchCD = hitcooldown;
		effect = "Deals damage on contact. C: Spawns 5 holograms that track enemies."; */
	}

	public override bool canShoot(int chargeLevel, Player player) {
		MegamanX? mmx = player.character as MegamanX;

		return base.canShoot(chargeLevel, player) && mmx?.sBodyHologram == null
			&& mmx?.sBodyClone == null;
	}

	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel >= 3) {
			character.changeState(new ControlClone(), true);
		} 
		else new SoulBodyHologram(this, pos, xDir, player, player.getNextActorNetId(), true);
	}
}


public class SoulBodyHologram : Projectile {

	MegamanX mmx = null!;
	float distance;
	const float maxDist = 64;
	int frameCount;
	public SoulBodyHologram(
		Weapon weapon, Point pos, int xDir, 
		Player player, ushort netProjId,  bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player,
		"empty", 0, 0.5f, netProjId,
		player.ownedByLocalPlayer
	) {
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		projId = (int)ProjIds.SoulBodyHologram;
		frameSpeed = 0;
		changeSprite(mmx.sprite.name, false);
		frameIndex = mmx.frameIndex;
		mmx.sBodyHologram = this;
		maxTime = 2f;
		setIndestructableProperties();
		canBeLocal = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SoulBodyHologram(
			SoulBody.netWeapon, arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
	
	public override void update() {
		base.update();
		globalCollider = new Collider(new Rect(0,0, 18, 34).getPoints(), 
			true, this, false, false, HitboxFlag.Hitbox, Point.zero);

		if (distance < maxDist) distance += 4;
		else distance = maxDist;

		xDir = mmx.xDir;

		changePos(mmx.pos.addxy(mmx.getShootXDir() * distance, 0));
		changeSprite(mmx.sprite.name, false);
		frameIndex = mmx.frameIndex;
		frameCount++;
	}
	
	public override List<ShaderWrapper>? getShaders() {
		var shaders = new List<ShaderWrapper>();

		ShaderWrapper cloneShader = Helpers.cloneShaderSafe("soulBodyPalette");

		int index = (frameCount / 2) % 7;
		if (index == 0) index++;

		cloneShader.SetUniform("palette", index);
		cloneShader.SetUniform("paletteTexture", Global.textures["soul_body_palette"]);
		shaders.Add(cloneShader);
	
		if (shaders.Count > 0) {
			return shaders;
		} else {
			return base.getShaders();
		}
	} 

	public override void onDestroy() {
		base.onDestroy();
		mmx.sBodyHologram = null!;
	}
}

public class ControlClone : CharState {

	MegamanX mmx = null!;
	bool fired;
	float cloneCooldown;
	int cloneCount;
	float[] altAngles = new float [] {0, 16, 240, 32, 224}; 

	public ControlClone() : base("summon") {
		normalCtrl = false;
		attackCtrl = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX ?? throw new NullReferenceException();
		mmx.useGravity = false;
		mmx.stopMoving();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.shootAnimTime = 0;
	}

	public override void update() {
		base.update();

		//x4Update();
		x5Update();
	}

	void x4Update() {
		if (character.isAnimOver() && !fired) {
			new SoulBodyClone(mmx.player, mmx.pos.x, mmx.pos.y, mmx.xDir, false,
			mmx.player.getNextATransNetId(), mmx.ownedByLocalPlayer);
			//mmx.ownedByLocalPlayer = false;

			fired = true;
			return;
		}
	}

	void x5Update() {
		Helpers.decrementFrames(ref cloneCooldown);

		if (cloneCount >= 5 && cloneCooldown <= 0) character.changeToIdleOrFall();

		else if (character.isAnimOver() && cloneCooldown <= 0) {
			float ang = altAngles[cloneCount];
			ang = character.xDir == 1 ? ang : -ang + 128;
			Actor? target = Global.level.getClosestTarget(character.getCenterPos(), player.alliance, false, 160);

			if (target != null) ang = character.pos.directionTo(target.getCenterPos()).byteAngle;

			new SoulBodyX5(
				new SoulBody(), character.pos, character.xDir,
				player, player.getNextActorNetId(), cloneCount + 1, ang, true
			) { releasePlasma = player.hasPlasma() && cloneCount == 0 };

			cloneCount++;
			cloneCooldown = 10;
		}
	}
}


public class SoulBodyX5 : Projectile {

	int color;

	public SoulBodyX5(
		Weapon weapon, Point pos, int xDir, Player player,
		ushort? netId, int color, float ang, bool rpc = false
	) : base (
		weapon, pos, 1, 300, 3, 
		player, "soul_body_x5", Global.halfFlinch, 0.75f,
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.SoulBodyX5;
		maxTime = 0.75f;
		vel = Point.createFromByteAngle(ang).times(speed);
		this.color = color;
		byteAngle = ang;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir, new byte[] {(byte)color, (byte)ang});
		} 
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SoulBodyX5(
			SoulBody.netWeapon, arg.pos, arg.xDir, arg.player, 
			arg.netId, arg.extraData[0], arg.extraData[1]
		);
	}

	public override List<ShaderWrapper>? getShaders() {
		var shaders = new List<ShaderWrapper>();

		ShaderWrapper cloneShader = Helpers.cloneShaderSafe("soulBodyPalette");

		cloneShader.SetUniform("palette", color);
		cloneShader.SetUniform("paletteTexture", Global.textures["soul_body_palette"]);
		shaders.Add(cloneShader);
	
		if (shaders.Count > 0) {
			return shaders;
		} else {
			return base.getShaders();
		}
	}	
}
