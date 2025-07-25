namespace BossMod.Shadowbringers.Foray.TheDalriada.DAL3SaunionDawon;

sealed class SpiralScourge(BossModule module) : Components.GenericAOEs(module)
{
    // Note: these spiral patterns do not seem to be hardcoded but can vary slightly (~1y), probably based on server ticks etc
    // amount of hits can also vary, usually there are 34, but there can be 33 or 35
    private static readonly AOEShapeCircle circle = new(14f); // increased radius by 1 to account for uncertainity
    private static readonly WPos[][] patterns = [
        [
            new(629.4802f, -679.0112f), new(629.4802f, -674.21985f), new(629.51086f, -669.0013f), new(629.51086f, -663.84375f),
            new(629.51086f, -658.6251f), new(629.51086f, -653.7423f), new(629.5719f, -648.8899f), new(629.5719f, -644.40375f),
            new(630.7926f, -640.9552f), new(633.9054f, -638.1781f), new(638.6052f, -637.20154f), new(642.2368f, -637.3236f),
            new(646.72314f, -638.819f), new(649.34766f, -642.08435f), new(649.9275f, -646.23486f), new(650.04944f, -650.8736f),
            new(650.04944f, -655.69543f), new(649.9884f, -660.48676f), new(649.9275f, -665.7053f), new(649.9579f, -670.5272f),
            new(650.4158f, -675.13544f), new(652.6741f, -678.645f), new(656.672f, -680.4761f), new(661.21924f, -680.6592f),
            new(665.64417f, -679.92676f), new(669.0012f, -676.9971f), new(670.40515f, -672.9381f), new(670.5271f, -668.1468f),
            new(670.5271f, -663.29443f), new(670.4966f, -658.50305f), new(670.46606f, -653.49817f), new(670.46606f, -648.6763f),
            new(670.46606f, -643.4882f), new(670.43555f, -639.27673f)
        ],
        [
            new(676.478f, -659.5102f), new(676.3865f, -655.4818f), new(675.59314f, -650.81256f), new(673.76196f, -646.4485f),
            new(671.0764f, -642.51166f), new(668.02466f, -638.97156f), new(664.05725f, -636.1029f), new(659.38806f, -633.78345f),
            new(655.1155f, -632.6848f), new(651.1787f, -632.5017f), new(646.5095f, -632.74585f), new(641.71814f, -633.6309f),
            new(637.6897f, -635.43146f), new(633.8445f, -638.3612f), new(630.6095f, -642.4506f), new(628.47327f, -646.38745f),
            new(626.6726f, -651.2703f), new(626.00134f, -655.72595f), new(626.55054f, -660.39526f), new(628.62585f, -664.6677f),
            new(631.18933f, -669.21497f), new(634.7295f, -672.11414f), new(638.7273f, -674.46405f), new(643.3356f, -675.8069f),
            new(648.0963f, -675.65424f), new(652.6741f, -674.31146f), new(656.672f, -671.0155f), new(658.7776f, -666.83453f),
            new(660.0288f, -662.501f), new(659.05225f, -658.19794f), new(656.09216f, -654.68835f), new(651.97217f, -653.04034f),
            new(648.3711f, -654.9935f), new(649.71387f, -658.6557f)
        ],
        [
            new(672.4802f, -679.3469f), new(667.81104f, -679.4385f), new(663.1417f, -679.4995f), new(658.5945f, -679.4995f),
            new(653.3761f, -679.4995f), new(648.5846f, -679.4995f), new(644.00696f, -679.53f), new(639.2156f, -679.56055f),
            new(634.3937f, -679.19434f), new(630.7926f, -677.2412f), new(628.7479f, -673.8842f), new(628.29016f, -669.0624f),
            new(628.77844f, -664.54565f), new(630.9453f, -660.88354f), new(634.66846f, -659.1135f), new(639.88696f, -658.9609f),
            new(644.8004f, -659.0219f), new(649.6223f, -659.0524f), new(654.38306f, -659.0524f), new(659.20496f, -659.0219f),
            new(663.87415f, -658.99146f), new(667.99414f, -657.7706f), new(670.985f, -654.4442f), new(671.7173f, -649.6834f),
            new(671.62573f, -645.9907f), new(669.7031f, -641.6876f), new(666.07153f, -639.0326f), new(662.4398f, -638.5443f),
            new(658.1062f, -638.5748f), new(653.1929f, -638.5748f), new(648.3711f, -638.5443f), new(643.15247f, -638.5443f),
            new(637.93384f, -638.5443f), new(632.71533f, -638.5138f), new(627.4967f, -638.5138f)
        ],
        [
            new(650.4768f, -685.5116f), new(654.90186f, -685.05383f), new(659.57117f, -683.98566f), new(663.87415f, -682.1241f),
            new(667.7195f, -679.22485f), new(670.95435f, -675.74585f), new(673.4568f, -671.99207f), new(675.44055f, -667.6585f),
            new(676.3865f, -662.9282f), new(676.4171f, -659.5407f), new(676.2949f, -654.65784f), new(675.13525f, -649.95807f),
            new(672.999f, -645.9297f), new(670.0083f, -642.1759f), new(665.94934f, -638.88f), new(661.76843f, -637.171f),
            new(657.46545f, -635.46204f), new(652.7351f, -635.1568f), new(648.0963f, -635.82825f), new(643.3661f, -638.08655f),
            new(639.4292f, -640.65f), new(636.0112f, -644.5258f), new(634.4243f, -649.0425f), new(633.08154f, -653.5287f),
            new(633.7223f, -658.2894f), new(635.49243f, -662.65356f), new(638.7273f, -665.919f), new(643.45764f, -668.11633f),
            new(647.4861f, -669.0624f), new(651.75854f, -667.5975f), new(654.9629f, -664.1489f), new(655.8784f, -660.21216f),
            new(653.6201f, -657.0687f)
        ],
        [
            new(650.4768f, -632.5017f), new(646.05164f, -632.6543f), new(641.41296f, -633.6004f), new(637.0488f, -635.523f),
            new(633.14246f, -638.11707f), new(629.84656f, -641.38245f), new(626.94727f, -644.98364f), new(624.7501f, -649.6223f),
            new(623.682f, -653.8644f), new(623.4989f, -657.80115f), new(623.5293f, -662.501f), new(624.6279f, -667.23126f),
            new(626.6422f, -671.29016f), new(629.29736f, -675.0439f), new(632.89844f, -678.18726f), new(636.9878f, -680.4761f),
            new(641.90125f, -682.1851f), new(646.72314f, -682.9786f), new(651.3618f, -682.4598f), new(655.6039f, -680.354f),
            new(660.151f, -677.7295f), new(663.20276f, -673.9757f), new(665.46106f, -669.88635f), new(666.926f, -665.30865f),
            new(666.6207f, -660.51733f), new(665.095f, -656.0006f), new(661.95154f, -652.7352f), new(657.3739f, -650.2022f),
            new(653.4674f, -648.9815f), new(648.8594f, -650.23267f), new(645.5939f, -653.254f), new(644.0985f, -657.0077f),
            new(645.6549f, -660.7615f), new(649.4696f, -659.205f)
        ],
        [
            new(627.4967f, -638.5138f), new(632.2881f, -638.5138f), new(637.4761f, -638.5443f), new(642.6642f, -638.5443f),
            new(647.8522f, -638.5443f), new(652.7656f, -638.5443f), new(657.2212f, -638.60535f), new(661.7075f, -638.5443f),
            new(665.21704f, -638.6969f), new(669.03174f, -640.9552f), new(671.2595f, -644.8616f), new(671.7478f, -648.9509f),
            new(671.229f, -653.2235f), new(668.72656f, -657.2213f), new(664.6067f, -658.89984f), new(660.334f, -659.0219f),
            new(655.6039f, -659.08295f), new(650.8125f, -659.0524f), new(645.9906f, -659.0219f), new(640.8026f, -658.9609f),
            new(635.9807f, -659.3271f), new(631.55554f, -660.54785f), new(628.931f, -663.4165f), new(628.3512f, -668.11633f),
            new(628.5648f, -672.63293f), new(629.96875f, -676.44775f), new(633.50867f, -678.88916f), new(637.96436f, -679.4995f),
            new(642.81665f, -679.4995f), new(647.60803f, -679.4995f), new(652.82666f, -679.469f), new(657.4043f, -679.4995f),
            new(662.6229f, -679.4995f), new(666.926f, -679.53f)
        ],
        [
            new(670.4966f, -638.0255f), new(670.4966f, -642.42004f), new(670.4966f, -646.57056f), new(670.5271f, -651.7586f),
            new(670.4966f, -656.27527f), new(670.46606f, -661.4939f), new(670.43555f, -666.07153f), new(670.43555f, -670.89343f),
            new(669.94727f, -675.2575f), new(667.50586f, -678.7976f), new(663.2943f, -680.354f), new(658.442f, -680.62866f),
            new(654.4136f, -679.8657f), new(651.2396f, -676.7224f), new(649.9884f, -672.81604f), new(649.9275f, -667.5975f),
            new(649.9579f, -662.9282f), new(650.01904f, -658.1064f), new(650.04944f, -652.9183f), new(649.9275f, -648.12695f),
            new(649.83594f, -643.4577f), new(648.27954f, -640.0397f), new(645.01404f, -637.72034f), new(641.07715f, -637.23206f),
            new(636.9573f, -637.3236f), new(632.59326f, -639.0631f), new(630.0907f, -642.3591f), new(629.4802f, -645.9907f),
            new(629.4802f, -650.8736f), new(629.51086f, -655.72595f), new(629.51086f, -660.9446f), new(629.51086f, -666.1021f),
            new(629.51086f, -671.3207f), new(629.4802f, -676.5088f)
        ],
        [
            new(623.4989f, -657.5265f), new(623.62085f, -661.95166f), new(624.29236f, -666.6209f), new(626.00134f, -670.95447f),
            new(628.4121f, -674.95233f), new(631.6776f, -678.4314f), new(635.645f, -681.84937f), new(639.9481f, -683.8941f),
            new(644.0679f, -685.23694f), new(647.97437f, -685.45056f), new(652.6741f, -685.359f), new(657.4348f, -684.56555f),
            new(661.95154f, -682.6734f), new(665.6748f, -679.71313f), new(669.06226f, -676.3257f), new(671.5648f, -672.14465f),
            new(673.02966f, -667.1092f), new(673.88416f, -662.47046f), new(673.33484f, -657.8317f), new(671.5342f, -653.4066f),
            new(668.94006f, -648.85944f), new(665.5221f, -645.9602f), new(661.5853f, -643.6713f), new(656.58044f, -642.2064f),
            new(652.2468f, -642.3591f), new(647.60803f, -643.61035f), new(643.5187f, -646.8452f), new(641.1687f, -650.7515f),
            new(640.0397f, -654.53577f), new(640.89404f, -659.205f), new(643.5492f, -663.1113f), new(647.7606f, -664.8204f),
            new(651.5449f, -663.35547f), new(650.75134f, -659.3271f)
        ]
    ];
    private readonly List<AOEInstance> _aoes = new(35);

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor)
    {
        var count = _aoes.Count;
        if (count == 0 || NumCasts >= count)
            return [];
        var max = count > 6 ? 6 : count;
        var aoes = CollectionsMarshal.AsSpan(_aoes);
        var maxC = Math.Min(max, count - NumCasts);
        if (NumCasts < count)
            aoes[NumCasts].Color = Colors.Danger;
        return aoes.Slice(NumCasts, maxC);
    }

    public override void OnCastStarted(Actor caster, ActorCastInfo spell)
    {
        if (spell.Action.ID == (uint)AID.SpiralScourgeVisual1)
        {
            NumCasts = 0;
            for (var i = 0; i < 6; ++i)
            {
                var pattern = patterns[i];
                if (pattern[0].AlmostEqual(spell.LocXZ, 1f))
                {
                    var len = pattern.Length;
                    var act = Module.CastFinishAt(spell, 0.8d);
                    for (var j = 0; j < len; ++j)
                    {
                        _aoes.Add(new(circle, pattern[j], default, act.AddSeconds(0.6d * i)));
                    }
                    return;
                }
            }
        }
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID == (uint)AID.SpiralScourge)
        {
            ++NumCasts;
        }
    }

    public override void OnActorDestroyed(Actor actor)
    {
        if (actor.OID is (uint)OID.SpiralPattern1 or (uint)OID.SpiralPattern2)
        {
            _aoes.Clear();
        }
    }
}
