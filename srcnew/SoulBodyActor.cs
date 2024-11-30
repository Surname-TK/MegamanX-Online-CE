using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SoulBodyClone : MegamanX {

	MegamanX owner = null!;
	Player pl = null!;
	float distance = 0;
	const float maxDist = 64;
	bool canMoveClone = false;
	float lifeTime = 300;
	public float maxHealth = 8;
	public float health = 8;
	bool plasma;
	public SoulBodyClone(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		owner = player.character as MegamanX ?? throw new NullReferenceException();
		owner.sBodyClone = this;
		player.sClone = this;
		pl = player;
		pos = owner.pos;
		changeState(new Idle(), true);
		base.player = pl;
		charId = CharIds.SoulBodyClone;

		player.clearXWeapons();
		player.weapons.Clear();
		player.weapons.Add(new Buster());
		player.changeWeaponSlot(0);
	}

	public override void update() {
		base.update();
		if (distance < maxDist) distance += 4;
		else {
			distance = maxDist;
			canMoveClone = true;
		} 

		if (!canMoveClone) changePos(owner.pos.addxy(owner.getShootXDir() * distance, 0));
		if (canMoveClone) Helpers.decrementFrames(ref lifeTime);

		if (lifeTime <= 0 || health <= 0) destroySelf();
	}

	public override bool canCharge() {
		return false;
	}

	public override bool canChangeWeapons() {
		return false;
	}

	public override void applyDamage(float fDamage, Player? attacker, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;
		decimal damage = decimal.Parse(fDamage.ToString());

		if (damage > 0 && actor != null && attacker != null && health > 0) {
			health -= fDamage;
			//playSound("hit", sendRpc: true);

			if (player.hasPlasma() && !plasma) {
				new BusterForcePlasmaHit(
					0, player.weapon, getCenterPos(), xDir, player,
					player.getNextActorNetId(), true
				);
				plasma = true;
			}
			
			base.applyDamage(fDamage, attacker, actor, weaponIndex, projId);
			return;
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;
		int index = player.weapon.index;

		if (index == (int)WeaponIds.GigaCrush ||
			index == (int)WeaponIds.ItemTracer ||
			index == (int)WeaponIds.AssassinBullet ||
			index == (int)WeaponIds.Undisguise ||
			index == (int)WeaponIds.UPParry
		) {
			index = 0;
		}
		if (index == (int)WeaponIds.HyperBuster && ownedByLocalPlayer) {
			index = player.weapons[player.hyperChargeSlot].index;
		}
		if (player.hasGoldenArmor()) {
			index = 25;
		}
		if (hasUltimateArmor) {
			index = 0;
		}
		palette = player.xPaletteShader;

		if (!isCStingInvisibleGraphics()) {
			palette?.SetUniform("palette", (int)WeaponIds.SoulBody);
			palette?.SetUniform("paletteTexture", Global.textures["paletteTexture"]);
		} else {
			palette?.SetUniform("palette", this.cStingPaletteIndex % 9);
			palette?.SetUniform("paletteTexture", Global.textures["cStingPalette"]);
		}
		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}
		shaders.AddRange(baseShaders);
		return shaders;
	}

	public override void onDestroy() {
		base.onDestroy();
		if (player.hasPlasma() && !plasma) {
			new BusterForcePlasmaHit(
				0, player.weapon, getCenterPos(), xDir, player,
				player.getNextActorNetId(), true
			);
			plasma = true;
		}
	}

	public override void destroySelf(string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false) {
		
		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);

		owner.ownedByLocalPlayer = true;
		owner.sBodyClone = null!;
		player.sClone = null!;
		owner.changeToIdleOrFall();
		owner.useGravity = true;
		player.weapons = new List<Weapon>(player.preXWeapons);
		player.preXWeapons.Clear();
		
	}	
}
