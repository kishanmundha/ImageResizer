using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageResizer
{
    public partial class FormStatus : Form
    {
        public FormStatus(List<ImageFile> files)
        {
            InitializeComponent();

            foreach(var f in files)
            {
                dataGridView1.Rows.Add(f.RelativePath, GetSuccesStatusMessage(f.SuccessStatus));
            }

            label1.Text = files.Where(x => x.SuccessStatus != -1).Count() + " Success";
            label2.Text = files.Where(x => x.SuccessStatus == -1).Count() + " Failed";
        }

        private string GetSuccesStatusMessage(int statusMsgId)
        {
            switch(statusMsgId)
            {
                case -1 :
                    return "Faild";
                case 0:
                    return "Success";
                case 1:
                    return "Success";
                default:
                    return "Unknonw status";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
