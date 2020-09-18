﻿using Remotely.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Remotely.Shared.Models.RemoteControlDtos
{
    [DataContract]
    public class MouseWheelDto : BaseDto
    {

        [DataMember(Name = "DtoType")]
        public new BinaryDtoType DtoType { get; } = BinaryDtoType.MouseWheel;

        [DataMember(Name = "DeltaX")]
        public double DeltaX { get; set; }

        [DataMember(Name = "DeltaY")]
        public double DeltaY { get; set; }
    }
}