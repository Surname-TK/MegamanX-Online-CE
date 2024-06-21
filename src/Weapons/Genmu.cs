﻿using System;

namespace MMXOnline;

public class Genmu : Weapon {
	public Genmu() : base() {
		index = (int)WeaponIds.Gemnu;
		killFeedIndex = 84;
	}
}

public class GenmuProj : Projectile {
	public int type = 0;
	public float initY = 0;

	public GenmuProj(Weapon weapon, Point pos, int xDir, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 300, 6, player, "genmu_proj", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		this.type = type;
		initY = pos.y;
		maxTime = 0.5f;
		destroyOnHit = false;
		xScale = 0.75f;
		yScale = 0.75f;
		projId = (int)ProjIds.Gemnu;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();

		float y = 0;
		if (type == 0) y = initY + MathF.Sin(time * 8) * 50;
		else y = initY + MathF.Sin(-time * 8) * 50;
		changePos(new Point(pos.x, y));
	}
}

public class GenmuState : CharState {
	bool fired;
	public GenmuState() : base("genmu", "", "", "") {
	}

	public override void update() {
		base.update();
		
		if (character.frameIndex >= 8 && !fired) {
			fired = true;
			character.playSound("genmureix5", sendRpc: true);
			new GenmuProj(new Genmu(), character.pos.addxy(30 * character.xDir, -25), character.xDir, 0, player, player.getNextActorNetId(), rpc: true);
			new GenmuProj(new Genmu(), character.pos.addxy(30 * character.xDir, -25), character.xDir, 1, player, player.getNextActorNetId(), rpc: true);
		}
		if (character.frameIndex == 1) {
			if (character is Zero zero) {
				if (zero.isNightmareZeroBS.getValue()) {
					character.playSound("znshine", sendRpc: true);					
				}
			}
		}
		if (character.isAnimOver()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
			if (character is Zero zero) {
				if (zero.isNightmareZeroBS.getValue()) {
					character.playSound("znsokomadeda", sendRpc: true);					
				}
			}
		}
	}
}
